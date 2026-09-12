namespace ProjectService.Application.Services;

/// <summary>
/// Интерфейс для работы с кэшем.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает значение из кэша по ключу.
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ кэша</param>
    /// <returns>Значение из кэша или null, если не найдено</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Сохраняет значение в кэш.
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="key">Ключ кэша</param>
    /// <param name="value">Значение</param>
    /// <param name="minutes">Время хранения в минутах</param>
    Task SetAsync<T>(string key, T value, int minutes = 5);

    /// <summary>
    /// Удаляет значение из кэша по ключу.
    /// </summary>
    /// <param name="key">Ключ кэша</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Удаляет все ключи, соответствующие шаблону.
    /// </summary>
    /// <param name="pattern">Шаблон ключа (например, "projects:*")</param>
    Task RemoveByPatternAsync(string pattern);
}
