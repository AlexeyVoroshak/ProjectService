using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectService.Application.Services;
using ProjectService.Infrastructure.ValueConversions;
using StackExchange.Redis;

namespace ProjectService.Infrastructure.Services;

/// <summary>
/// Реализация ICacheService на базе Redis.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerSettings _jsonSettings;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="connection">Подключение к Redis</param>
    /// <param name="logger">Логгер</param>
    public RedisCacheService(IConnectionMultiplexer connection, ILogger<RedisCacheService> logger)
    {
        _database = connection.GetDatabase();
        _logger = logger;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new ProjectStatusConverter(),
                new TaskStatusConverter(),
                new TaskPriorityConverter()
            }
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _database.StringGetAsync(key);
            if (data.IsNullOrEmpty)
            {
                return default;
            }

            var value = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(value, _jsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка получения данных из кэша по ключу {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, int minutes = 5)
    {
        try
        {
            if (value == null)
            {
                return;
            }

            var serialized = JsonConvert.SerializeObject(value, _jsonSettings);
            var bytes = Encoding.UTF8.GetBytes(serialized);
            await _database.StringSetAsync(key, bytes, TimeSpan.FromMinutes(minutes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка сохранения данных в кэш с ключом {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка удаления ключа из кэша {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = GetRedisServer();
            var keys = server.Keys(pattern: pattern).Cast<RedisKey>();

            if (keys.Any())
            {
                await _database.KeyDeleteAsync(keys.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка удаления ключей по шаблону {Pattern}", pattern);
        }
    }

    private IServer GetRedisServer()
    {
        var endpoints = _database.Multiplexer.GetEndPoints();
        var endpoint = endpoints.FirstOrDefault();
        if (endpoint == null)
        {
            throw new InvalidOperationException("Redis connection has no endpoints");
        }

        return _database.Multiplexer.GetServer(endpoint);
    }
}
