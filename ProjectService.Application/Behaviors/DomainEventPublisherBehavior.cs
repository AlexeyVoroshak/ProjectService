using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectService.Application.Services;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Events;

namespace ProjectService.Application.Behaviors;

/// <summary>
/// Pipeline behavior для перехвата доменных событий и сохранения их в Outbox.
/// 
/// Этот обработчик встраивается в конвейер MediatR и работает следующим образом:
/// 1. После выполнения команды (handler) MediatR проверяет, есть ли доменные события (INotification).
/// 2. Если события есть, behavior сериализует каждое событие в JSON.
/// 3. Сериализованные события сохраняются в таблицу OutboxMessages в БД.
/// 4. Важность: это происходит в той же транзакции, что и основная доменная операция,
///    гарантируя атомарность — либо оба действия успешны, либо оба откатываются.
/// 
/// Background worker (KafkaOutboxPublisher) периодически опрашивает таблицу OutboxMessages
/// и публикует неподтверждённые сообщения в Kafka.
/// </summary>
public class DomainEventPublisherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private readonly ApplicationDBContext _dbContext;
    private readonly ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="dbContext">Контекст EF Core для записи в Outbox</param>
    /// <param name="logger">Логгер</param>
    public DomainEventPublisherBehavior(ApplicationDBContext dbContext, ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает запрос, проверяя наличие доменных событий после выполнения handler.
    /// </summary>
    /// <param name="request">Входящий запрос/команда</param>
    /// <param name="next">Следующий обработчик в конвейере</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат выполнения</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Выполняем следующий обработчик в конвейере (т.е. сам handler команды/запроса)
        var response = await next();

        // Получаем все доменные события из запроса (если запрос — это событие)
        var domainEvents = GetDomainEvents(request);

        // Если доменных событий нет — возвращаем результат без изменений
        if (!domainEvents.Any())
            return response;

        // Сериализуем каждое событие и сохраняем в Outbox
        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventToOutboxAsync(domainEvent, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Извлекает доменные события из объекта (запроса или результата).
    /// Поддерживает INotification (MediatR) и агрегаты с DomainEvents.
    /// </summary>
    private static IEnumerable<object> GetDomainEvents(object obj)
    {
        // Проверяем, является ли объект INotification (MediatR)
        if (obj is INotification notification)
        {
            yield return notification;
            yield break;
        }

        // Проверяем, является ли объект агрегатом с доменными событиями
        if (obj is Domain.Entities.BaseEntity baseEntity)
        {
            var eventsProperty = baseEntity.GetType().GetProperty("DomainEvents");
            if (eventsProperty != null)
            {
                var events = eventsProperty.GetValue(baseEntity) as IEnumerable<object>;
                if (events != null)
                {
                    foreach (var evt in events)
                    {
                        yield return evt;
                    }
                }
            }
            yield break;
        }

        // Проверяем, является ли объект коллекцией агрегатов
        if (obj is IEnumerable<Domain.Entities.BaseEntity> entityCollection)
        {
            foreach (var aggregateEntity in entityCollection)
            {
                var eventsProperty = aggregateEntity.GetType().GetProperty("DomainEvents");
                if (eventsProperty != null)
                {
                    var events = eventsProperty.GetValue(aggregateEntity) as IEnumerable<object>;
                    if (events != null)
                    {
                        foreach (var evt in events)
                        {
                            yield return evt;
                        }
                    }
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Сериализует доменное событие и сохраняет его в Outbox через DbContext.
    /// Запись происходит в рамках текущей транзакции БД.
    /// </summary>
    private async Task PublishDomainEventToOutboxAsync(object domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            // Определяем aggregate ID из события (извлекаем из свойства, содержащего сущность)
            var aggregateId = ExtractAggregateId(domainEvent);
            var eventType = domainEvent.GetType().FullName ?? typeof(object).FullName;
            var payload = JsonConvert.SerializeObject(domainEvent);

            var outboxMessage = OutboxMessage.Create(aggregateId, eventType, payload);
            _dbContext.OutboxMessages.Add(outboxMessage);

            _logger.LogInformation(
                "Доменное событие сохранено в Outbox: {EventType}, AggregateId: {AggregateId}",
                eventType, aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении доменного события в Outbox: {EventType}", domainEvent.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Извлекает идентификатор агрегата из доменного события.
    /// Ожидается, что событие имеет свойство с типом BaseEntity (например, Project или TaskEntity).
    /// </summary>
    private static string ExtractAggregateId(object domainEvent)
    {
        var eventType = domainEvent.GetType();
        // Ищем свойство, тип которого наследуется от BaseEntity
        var property = eventType.GetProperties()
            .FirstOrDefault(p => typeof(Domain.Entities.BaseEntity).IsAssignableFrom(p.PropertyType));

        if (property != null)
        {
            var entity = property.GetValue(domainEvent) as Domain.Entities.BaseEntity;
            return entity?.Id.ToString() ?? Guid.Empty.ToString();
        }

        return Guid.Empty.ToString();
    }
}
