using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProjectService.Infrastructure.Services;

/// <summary>
/// Фоновая служба для инициализации и проверки подключения к Redis.
/// Запускается при старте приложения и проверяет доступность Redis.
/// </summary>
public class RedisInitializationService : IHostedService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisInitializationService> _logger;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="connection">Подключение к Redis</param>
    /// <param name="options">Опции Redis</param>
    /// <param name="logger">Логгер</param>
    public RedisInitializationService(
        IConnectionMultiplexer connection,
        RedisOptions options,
        ILogger<RedisInitializationService> logger)
    {
        _connection = connection;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Запускается при старте приложения.
    /// Проверяет подключение к Redis и готовность к работе.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Инициализация подключения к Redis...");

        try
        {
            // Проверяем подключение к Redis
            var server = GetPrimaryServer();
            var ping = await server.PingAsync();

            _logger.LogInformation(
                "Подключение к Redis успешно установлено. Ping: {Ping}ms",
                ping.TotalMilliseconds);

            // Проверяем доступность выбранной базы данных
            var db = _connection.GetDatabase(_options.DatabaseIndex);
            _logger.LogInformation("Используемая база данных Redis: {DatabaseIndex}", _options.DatabaseIndex);
            await db.StringSetAsync("__redis_init__", "1", TimeSpan.FromSeconds(1));
            var value = await db.StringGetAsync("__redis_init__");

            if (value.HasValue)
            {
                await db.KeyDeleteAsync("__redis_init__");
                _logger.LogInformation("База данных Redis {DatabaseIndex} доступна и готова к работе", _options.DatabaseIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Не удалось подключиться к Redis. Кэширование будет недоступно. " +
                "Убедитесь, что Redis запущен и доступен по адресу конфигурации.");
        }
    }

    /// <summary>
    /// Останавливается при остановке приложения.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка подключения к Redis");
        return Task.CompletedTask;
    }

    private IServer GetPrimaryServer()
    {
        var endpoints = _connection.GetEndPoints();
        var endpoint = endpoints.FirstOrDefault()
            ?? throw new InvalidOperationException("Redis connection has no endpoints configured");

        return _connection.GetServer(endpoint);
    }
}
