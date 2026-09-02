namespace ProjectService.Domain.Enums;

/// <summary>
/// Статус задачи.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Задача не начата.
    /// </summary>
    Todo = 1,

    /// <summary>
    /// Задача в процессе выполнения.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Задача завершена.
    /// </summary>
    Done = 3,

    /// <summary>
    /// Задача отменена.
    /// </summary>
    Cancelled = 4
}
