namespace ProjectService.Domain.Enums;

/// <summary>
/// Приоритет задачи.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Низкий приоритет.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Средний приоритет.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Высокий приоритет.
    /// </summary>
    High = 3,

    /// <summary>
    /// Критический приоритет.
    /// </summary>
    Critical = 4
}
