using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Events;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Behaviors;

/// <summary>
/// JSON настройки для предотвращения циклических ссылок.
/// </summary>
public static class JsonSettings
{
    public static JsonSerializerSettings Default => new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };
}

/// <summary>
/// Pipeline behavior для перехвата доменных событий и сохранения их в Outbox.
/// 
/// Этот обработчик встраивается в конвейер MediatR и работает следующим образом:
/// 1. После выполнения команды (handler) MediatR проверяет ChangeTracker DbContext
///    на наличие сущностей с доменными событиями.
/// 2. Если события есть, behavior сериализует каждое событие в JSON.
/// 3. Сериализованные события сохраняются в таблицу OutboxMessages в БД.
/// 4. После публикации доменные события очищаются из агрегатов.
/// 5. Это происходит в той же транзакции, что и основная доменная операция,
///    гарантируя атомарность — либо оба действия успешны, либо оба откатываются.
/// 
/// Background worker (KafkaOutboxPublisher) периодически опрашивает таблицу OutboxMessages
/// и публикует неподтверждённые сообщения в Kafka.
/// </summary>
public class DomainEventPublisherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="unitOfWork">Unit of Work для доступа к DbContext</param>
    /// <param name="logger">Логгер</param>
    public DomainEventPublisherBehavior(IUnitOfWork unitOfWork, ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
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

        // Получаем все доменные события из отслеживаемых сущностей в ChangeTracker
        var domainEvents = GetDomainEventsFromChangeTracker();

        // Если доменных событий нет — возвращаем результат без изменений
        if (!domainEvents.Any())
            return response;

        // Сериализуем каждое событие и сохраняем в Outbox
        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventToOutboxAsync(domainEvent, cancellationToken);
        }

        // Очищаем доменные события после публикации
        ClearDomainEventsFromChangeTracker();

        return response;
    }

    /// <summary>
    /// Извлекает доменные события из всех отслеживаемых сущностей в ChangeTracker.
    /// Проверяет только сущности, которые были добавлены или обновлены.
    /// </summary>
    private object[] GetDomainEventsFromChangeTracker()
    {
        var entries = _unitOfWork.DbContext.ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            // Проверяем только добавленные или изменённые сущности
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                return GetDomainEventsFromEntity(entry.Entity).ToArray();
            }
        }

        return Array.Empty<object>();
    }

    /// <summary>
    /// Извлекает доменные события из агрегата.
    /// </summary>
    private static IEnumerable<object> GetDomainEventsFromEntity(BaseEntity entity)
    {
        var eventsProperty = entity.GetType().GetProperty("DomainEvents");
        if (eventsProperty == null)
            yield break;

        var events = eventsProperty.GetValue(entity) as IEnumerable<object>;
        if (events == null)
            yield break;

        foreach (var evt in events)
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Очищает доменные события из всех отслеживаемых сущностей.
    /// </summary>
    private void ClearDomainEventsFromChangeTracker()
    {
        var entries = _unitOfWork.DbContext.ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            entry.Entity.ClearDomainEvents();
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
            var payload = JsonConvert.SerializeObject(domainEvent, JsonSettings.Default);

            var outboxMessage = OutboxMessage.Create(aggregateId, eventType, payload);
            _unitOfWork.Set<OutboxMessage>().Add(outboxMessage);

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
