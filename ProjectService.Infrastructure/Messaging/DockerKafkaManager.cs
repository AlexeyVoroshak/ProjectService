using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectService.Infrastructure.Messaging;

/// <summary>
/// Конфигурация Docker для автоматического запуска Kafka.
/// </summary>
public class DockerKafkaOptions
{
    /// <summary>
    /// Включить автоматический запуск Kafka через Docker SDK.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Управляет Docker контейнерами Kafka через Docker SDK.
/// Проверяет и запускает Zookeeper + Kafka при отсутствии запущенного кластера.
/// </summary>
public class DockerKafkaManager : IHostedService
{
    private const string ZookeeperImage = "confluentinc/cp-zookeeper:7.6.1";
    private const string KafkaImage = "confluentinc/cp-kafka:7.6.1";
    private const string NetworkName = "ProjectService_network";

    private readonly DockerKafkaOptions _options;
    private readonly ILogger<DockerKafkaManager> _logger;
    private readonly IHostEnvironment _environment;
    private readonly DockerClient _dockerClient;

    public DockerKafkaManager(
        IOptions<DockerKafkaOptions> options,
        ILogger<DockerKafkaManager> logger,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _logger = logger;
        _environment = environment;

        // Подключаемся к Docker daemon
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    /// <summary>
    /// Проверяет наличие запущенного Kafka и запускает его при необходимости.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Работаем только в Development режиме
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation("DockerKafkaManager пропускает проверку (не Development режим)");
            return;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation("DockerKafkaManager отключён через конфигурацию");
            return;
        }

        try
        {
            _logger.LogInformation("Проверка запущенного кластера Kafka через Docker SDK...");

            // Проверяем, запущены ли контейнеры
            var kafkaContainer = await FindContainerByNameAsync("kafka", cancellationToken);
            
            if (kafkaContainer != null && IsContainerRunning(kafkaContainer))
            {
                _logger.LogInformation("Кластер Kafka уже запущен");
                return;
            }

            _logger.LogInformation("Кластер Kafka не найден. Запуск через Docker SDK...");
            await StartKafkaClusterAsync(cancellationToken);
            
            _logger.LogInformation("Кластер Kafka успешно запущен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при управлении кластером Kafka через Docker");
            // Не выбрасываем исключение — приложение может продолжить работу
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
    /// Запускает кластер Kafka (Zookeeper + Kafka).
    /// </summary>
    private async Task StartKafkaClusterAsync(CancellationToken cancellationToken)
    {
        // 1. Создаём сеть, если не существует
        await EnsureNetworkAsync(cancellationToken);

        // 2. Запускаем Zookeeper
        await StartZookeeperAsync(cancellationToken);

        // 3. Ждём пока Zookeeper запустится
        _logger.LogInformation("Ожидание запуска Zookeeper...");
        await WaitForContainerRunningAsync("projectservice-zookeeper", cancellationToken, TimeSpan.FromSeconds(30));

        // 4. Запускаем Kafka
        await StartKafkaAsync(cancellationToken);

        // 5. Ждём пока Kafka запустится
        _logger.LogInformation("Ожидание запуска Kafka...");
        await WaitForContainerRunningAsync("projectservice-kafka", cancellationToken, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Создаёт Docker сеть, если не существует.
    /// </summary>
    private async Task EnsureNetworkAsync(CancellationToken cancellationToken)
    {
        try
        {
            var networkParams = new NetworksListParameters
            {
                Filters = (IDictionary<string, IDictionary<string, bool>>) new Dictionary<string, Dictionary<string, bool>> 
                { 
                    {
                        "name",
                        new Dictionary<string, bool>
                        { 
                           { NetworkName, true } 
                        } 
                    }
                }
            };

            var networks = await _dockerClient.Networks.ListNetworksAsync(networkParams, cancellationToken);

            if (networks.Any())
            {
                _logger.LogInformation("Docker сеть '{Network}' уже существует", NetworkName);
                return;
            }

            _logger.LogInformation("Создание Docker сети '{Network}'...", NetworkName);
            
            await _dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters { Name = NetworkName, Driver = "bridge" },
                cancellationToken);

            _logger.LogInformation("Docker сеть '{Network}' создана", NetworkName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Docker сеть '{Network}' уже существует", NetworkName);
        }
    }

    /// <summary>
    /// Запускает контейнер Zookeeper.
    /// </summary>
    private async Task StartZookeeperAsync(CancellationToken cancellationToken)
    {
        var existingContainer = await FindContainerByNameAsync("projectservice-zookeeper", cancellationToken);

        if (existingContainer != null)
        {
            if (IsContainerRunning(existingContainer))
            {
                _logger.LogInformation("Zookeeper уже запущен");
                return;
            }

            // Контейнер существует, но не запущен — запускаем его
            _logger.LogInformation("Запуск существующего контейнера Zookeeper...");
            await _dockerClient.Containers.StartContainerAsync("projectservice-zookeeper", new ContainerStartParameters(), cancellationToken);
            return;
        }

        // Создаём новый контейнер
        _logger.LogInformation("Создание контейнера Zookeeper из образа '{Image}'...", ZookeeperImage);

        var response = await _dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = ZookeeperImage,
                Name = "projectservice-zookeeper",
                Env = new[]
                {
                    "ZOOKEEPER_CLIENT_PORT=2181",
                    "ZOOKEEPER_TICK_TIME=2000"
                },
                HostConfig = new HostConfig
                {
                    NetworkMode = NetworkName
                },
                Hostname = "zookeeper"
            },
            cancellationToken);

        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);
        _logger.LogInformation("Контейнер Zookeeper создан и запущен");
    }

    /// <summary>
    /// Запускает контейнер Kafka.
    /// </summary>
    private async Task StartKafkaAsync(CancellationToken cancellationToken)
    {
        var existingContainer = await FindContainerByNameAsync("projectservice-kafka", cancellationToken);

        if (existingContainer != null)
        {
            if (IsContainerRunning(existingContainer))
            {
                _logger.LogInformation("Kafka уже запущен");
                return;
            }

            // Контейнер существует, но не запущен — запускаем его
            _logger.LogInformation("Запуск существующего контейнера Kafka...");
            await _dockerClient.Containers.StartContainerAsync("projectservice-kafka", new ContainerStartParameters(), cancellationToken);
            return;
        }

        // Создаём новый контейнер
        _logger.LogInformation("Создание контейнера Kafka из образа '{Image}'...", KafkaImage);

        var response = await _dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = KafkaImage,
                Name = "projectservice-kafka",
                Env = new[]
                {
                    "KAFKA_BROKER_ID=1",
                    "KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181",
                    "KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092",
                    "KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=PLAINTEXT:PLAINTEXT",
                    "KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1",
                    "KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1",
                    "KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1"
                },
                HostConfig = new HostConfig
                {
                    NetworkMode = NetworkName,
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "9092/tcp",
                            new List<PortBinding> { new PortBinding { HostPort = "9092" } }
                        }
                    }
                },
                Hostname = "kafka"
            },
            cancellationToken);

        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancellationToken);
        _logger.LogInformation("Контейнер Kafka создан и запущен");
    }

    /// <summary>
    /// Ищет контейнер по имени.
    /// </summary>
    private async Task<ContainerListResponse?> FindContainerByNameAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, cancellationToken);

            return containers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при поиске контейнера '{Name}'", name);
            return null;
        }
    }

    /// <summary>
    /// Проверяет, запущен ли контейнер.
    /// </summary>
    private static bool IsContainerRunning(ContainerListResponse? container)
    {
        return container != null && container.State?.Equals("running", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Ожидает запуска контейнера.
    /// </summary>
    private async Task WaitForContainerRunningAsync(string containerName, CancellationToken cancellationToken, TimeSpan timeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                var container = await FindContainerByNameAsync(containerName, cancellationToken);
                if (container?.State?.Equals("running", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogInformation("Контейнер '{ContainerName}' запущен", containerName);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при проверке статуса контейнера '{ContainerName}'", containerName);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        _logger.LogWarning("Таймаут ожидания запуска контейнера '{ContainerName}'", containerName);
    }
}
