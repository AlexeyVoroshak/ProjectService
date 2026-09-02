using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectService.Infrastructure.Messaging;

/// <summary>
/// Инициализирует топики Kafka при запуске приложения.
/// Проверяет доступность брокера и создаёт топики, если они не существуют.
/// </summary>
public class KafkaTopicInitializer : IHostedService
{
    private const int MaxRetries = 15;
    private const int InitialRetryDelayMs = 2000;

    private readonly KafkaOutboxOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IOptions<KafkaOutboxOptions> options, ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Создаёт топики Kafka при запуске приложения.
    /// Проверяет доступность брокера Kafka перед созданием топиков.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Инициализация топиков Kafka. BootstrapServers: {Servers}", _options.BootstrapServers);

        try
        {
            // Проверяем доступность брокера Kafka с повторными попытками
            var brokerAvailable = await WaitForBrokerAsync(cancellationToken);
            
            if (!brokerAvailable)
            {
                _logger.LogWarning(
                    "Брокер Kafka недоступен после {RetryCount} попыток. Топики не будут созданы.",
                    MaxRetries);
                return;
            }

            _logger.LogInformation("Брокер Kafka доступен. Создание топиков...");
            await CreateTopicsAsync(cancellationToken);
            _logger.LogInformation("Топики Kafka успешно инициализированы");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при инициализации топиков Kafka");
            // Не выбрасываем исключение, чтобы не блокировать запуск приложения
        }
    }

    /// <summary>
    /// Остановка не требует дополнительной логики.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ожидает доступность брокера Kafka с повторными попытками и экспоненциальной задержкой.
    /// </summary>
    private async Task<bool> WaitForBrokerAsync(CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        var delayMs = InitialRetryDelayMs;

        for (var i = 0; i < MaxRetries; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                _logger.LogInformation(
                    "Проверка доступности брокера Kafka (попытка {Attempt}/{MaxRetries})...",
                    i + 1,
                    MaxRetries);

                using var adminClient = new AdminClientBuilder(adminConfig).Build();
                
                // Пытаемся получить метаданные — это проверит подключение к брокеру
                var metadata = adminClient.GetMetadata(
                    TimeSpan.FromSeconds(10));

                _logger.LogInformation(
                    "Брокер Kafka доступен. Brokers: {BrokerCount}",
                    metadata.Brokers.Count);

                return true;
            }
            catch (KafkaException ex) when (ex.Error.IsError)
            {
                // Обрабатываем специфичные ошибки Kafka
                _logger.LogWarning(
                    "Ошибка подключения к Kafka (попытка {Attempt}/{MaxRetries}): {ErrorCode} - {Reason}",
                    i + 1,
                    MaxRetries,
                    ex.Error.Code,
                    ex.Error.Reason);

                // Broker transport failure — временная ошибка, продолжаем попытки
                await DelayWithBackoff(delayMs, cancellationToken);
                delayMs = Math.Min(delayMs * 2, 10000); // экспоненциальный рост до 10 сек
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Не удалось подключиться к брокеру Kafka (попытка {Attempt}/{MaxRetries})",
                    i + 1,
                    MaxRetries);

                await DelayWithBackoff(delayMs, cancellationToken);
                delayMs = Math.Min(delayMs * 2, 10000);
            }
        }

        return false;
    }

    /// <summary>
    /// Задержка с экспоненциальным backoff.
    /// </summary>
    private async Task DelayWithBackoff(int delayMs, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ожидание {DelayMs}мс перед следующей попыткой...", delayMs);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
    }

    /// <summary>
    /// Создаёт необходимые топики в Kafka.
    /// </summary>
    private async Task CreateTopicsAsync(CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new List<TopicSpecification>();

        // Проверяем и создаём основной топик
        if (!await IsTopicExistsAsync(adminClient, _options.DefaultTopic))
        {
            topicsToCreate.Add(new TopicSpecification
            {
                Name = _options.DefaultTopic,
                NumPartitions = 3,
                ReplicationFactor = 1
            });
        }

        // Проверяем и создаём DLQ топик
        if (!await IsTopicExistsAsync(adminClient, _options.DlqTopic))
        {
            topicsToCreate.Add(new TopicSpecification
            {
                Name = _options.DlqTopic,
                NumPartitions = 1,
                ReplicationFactor = 1
            });
        }

        // Создаём топики, если нужно
        if (topicsToCreate.Count > 0)
        {
            _logger.LogInformation("Создание {Count} топиков в Kafka", topicsToCreate.Count);
            
            try
            {
                var options = new CreateTopicsOptions();
                options.OperationTimeout = TimeSpan.FromSeconds(30);

                await adminClient.CreateTopicsAsync(
                    topicsToCreate.ToArray(),
                    options);

                foreach (var topic in topicsToCreate)
                {
                    _logger.LogInformation("Топик '{Topic}' успешно создан", topic.Name);
                }
            }
            catch (CreateTopicsException ex)
            {
                // Обрабатываем ошибки создания
                foreach (var result in ex.Results)
                {
                    if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                    {
                        _logger.LogError(
                            "Ошибка при создании топика '{Topic}': {Error}",
                            result.Topic,
                            result.Error.Reason);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Топик '{Topic}' уже существует",
                            result.Topic);
                    }
                }
            }
        }
        else
        {
            _logger.LogInformation(
                "Все топики ('{DefaultTopic}' и '{DlqTopic}') уже существуют",
                _options.DefaultTopic,
                _options.DlqTopic);
        }
    }

    /// <summary>
    /// Проверяет, существует ли топик в Kafka.
    /// </summary>
    private async Task<bool> IsTopicExistsAsync(
        IAdminClient adminClient, 
        string topicName)
    {
        try
        {
            // Получаем метаданные кластера
            var metadata = adminClient.GetMetadata(
                TimeSpan.FromSeconds(10));

            return metadata.Topics.Any(t => 
                t.Topic == topicName && 
                t.Partitions.Count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Не удалось проверить существование топика '{Topic}'", topicName);
            // Если не можем проверить, считаем что топик не существует
            return false;
        }
    }
}
