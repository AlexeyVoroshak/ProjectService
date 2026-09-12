namespace ProjectService.Infrastructure.Services;

/// <summary>
/// Параметры конфигурации Redis.
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Индекс базы данных Redis (0-15).
    /// </summary>
    public int DatabaseIndex { get; set; }
}
