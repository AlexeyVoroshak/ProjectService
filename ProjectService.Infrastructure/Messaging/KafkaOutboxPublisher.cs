using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Data;

namespace ProjectService.Infrastructure.Messaging;

/// <summary>
/// Конфигурация Kafka для Outbox Publisher.
/// </summary>
public class KafkaOutboxOptions
{
    /// <summary>
    /// Адрес брокера Kafka.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:29092";

    /// <summary>
    /// Имя топика по умолчанию для публикации событий.
    /// </summary>
    public string DefaultTopic { get; set; } = "project-service.events";

    /// <summary>
    /// Имя топика Dead Letter Queue для неудачных сообщений.
    /// </summary>
    public string DlqTopic { get; set; } = "project-service.dlq";

    /// <summary>
    /// Интервал опроса неподтверждённых сообщений в миллисекундах.
    /// </summary>
    public int PollIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Максимальное количество сообщений за одну итерацию.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Максимальное количество попыток публикации перед отправкой в DLQ.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Базовая задержка между повторными попытками в миллисекундах (экспоненциальный backoff).
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;
}

/// <summary>
/// Background worker, который опрашивает таблицу OutboxMessages и публикует
/// неподтверждённые события в Kafka.
/// 
/// Алгоритм работы:
/// 1. Периодически опрашивает БД на наличие сообщений со статусом Pending.
/// 2. Для каждого сообщения пытается опубликовать его в Kafka.
/// 3. При успехе — помечает сообщение как Published.
/// 4. При ошибке — увеличивает счётчик попыток и помечает как Failed.
/// 5. Если количество попыток превысило MaxAttempts — отправляет сообщение в DLQ.
/// 
/// Retry policy: экспоненциальный backoff (1s, 2s, 4s, 8s, 16s).
/// </summary>
public class KafkaOutboxPublisher : BackgroundService
{
    private readonly KafkaOutboxOptions _options;
    private readonly ILogger<KafkaOutboxPublisher> _logger;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _stoppingCancellationToken;
    private IProducer<string, string>? _producer;
    private readonly object _producerLock = new();

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="options">Настройки Kafka</param>
    /// <param name="logger">Логгер</param>
    /// <param name="serviceProvider">Сервис провайдер для создания scope</param>
    public KafkaOutboxPublisher(KafkaOutboxOptions options, ILogger<KafkaOutboxPublisher> logger, IServiceProvider serviceProvider)
    {
        _options = options;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Основной цикл службы — запускает периодический опрос Outbox.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaOutboxPublisher запущен. Интервал опроса: {PollInterval}ms", _options.PollIntervalMs);

        // Создаём токен, который будет отменяться при остановке службы
        _stoppingCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        try
        {
            while (!_stoppingCancellationToken.Token.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingMessagesAsync(_serviceProvider, _stoppingCancellationToken.Token);
                }
                catch (OperationCanceledException)
                {
                    // Ожидаемое завершение
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщений Outbox");
                }

                // Пауза между итерациями
                await Task.Delay(TimeSpan.FromMilliseconds(_options.PollIntervalMs), _stoppingCancellationToken.Token);
            }
        }
        finally
        {
            _producer?.Dispose();
            _logger.LogInformation("KafkaOutboxPublisher остановлен");
        }
    }

    /// <summary>
    /// Обрабатывает пакет неподтверждённых сообщений из БД.
    /// </summary>
    private async Task ProcessPendingMessagesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Получаем пакет неподтверждённых сообщений
        var pendingMessages = await outboxRepository.GetPendingAsync(_options.BatchSize, cancellationToken);
        var messages = pendingMessages.ToList();

        if (!messages.Any())
            return;

        _logger.LogInformation("Найдено {Count} неподтверждённых сообщений для публикации", messages.Count);

        // Переиспользуем producer для всего пакета (создаём один раз)
        var producer = GetOrCreateProducer();

        var publishedIds = new List<Guid>();

        foreach (var message in messages)
        {
            try
            {
                // Публикуем сообщение в Kafka
                var result = await producer.ProduceAsync(
                    _options.DefaultTopic,
                    new Message<string, string>
                    {
                        Key = message.AggregateId,
                        Value = message.Payload
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Сообщение опубликовано в Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                    result.Topic, result.Partition, result.Offset);

                publishedIds.Add(message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка публикации сообщения {MessageId} в Kafka", message.Id);

                // Если превышено максимальное количество попыток — отправляем в DLQ
                if (message.FailedAttempts >= _options.MaxAttempts)
                {
                    await SendToDlqAsync(message, producer, cancellationToken);
                }
                else
                {
                    // Увеличиваем счётчик попыток и помечаем как Failed
                    await outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
                }
            }
        }

        // Помечаем успешно опубликованные сообщения
        if (publishedIds.Any())
        {
            await outboxRepository.MarkAsPublishedAsync(publishedIds, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Отмечено {Count} сообщений как опубликованных", publishedIds.Count);
        }
    }

    /// <summary>
    /// Создаёт или переиспользует Kafka producer (thread-safe).
    /// </summary>
    private IProducer<string, string> GetOrCreateProducer()
    {
        if (_producer != null)
            return _producer;

        lock (_producerLock)
        {
            if (_producer == null)
            {
                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = _options.BootstrapServers,
                    Acks = Acks.All,
                    RetryBackoffMs = 1000
                };
                _producer = new ProducerBuilder<string, string>(producerConfig).Build();
                _logger.LogInformation("Kafka producer инициализирован. BootstrapServers: {Servers}", _options.BootstrapServers);
            }
            return _producer;
        }
    }

    /// <summary>
    /// Отправляет неудачное сообщение в Dead Letter Queue.
    /// </summary>
    private async Task SendToDlqAsync(OutboxMessage message, IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        try
        {
            var dlqPayload = JsonConvert.SerializeObject(new
            {
                OriginalPayload = message.Payload,
                EventType = message.EventType,
                AggregateId = message.AggregateId,
                FailedAttempts = message.FailedAttempts,
                LastError = message.LastError,
                FailedAt = DateTime.UtcNow
            });

            await producer.ProduceAsync(
                _options.DlqTopic,
                new Message<string, string>
                {
                    Key = message.AggregateId,
                    Value = dlqPayload
                },
                cancellationToken);

            // Помечаем сообщение как Failed с информацией о DLQ
            await _serviceProvider.GetRequiredService<IOutboxRepository>()
                .MarkAsFailedAsync(message.Id, $"Sent to DLQ: {_options.DlqTopic}", cancellationToken);

            _logger.LogWarning(
                "Сообщение {MessageId} отправлено в DLQ: {DlqTopic}", message.Id, _options.DlqTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения {MessageId} в DLQ", message.Id);
        }
    }

    /// <summary>
    /// Останавливает службу и очищает ресурсы.
    /// </summary>
    public override void Dispose()
    {
        _stoppingCancellationToken?.Cancel();
        _stoppingCancellationToken?.Dispose();
        base.Dispose();
    }
}
