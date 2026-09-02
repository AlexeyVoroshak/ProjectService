using ProjectService.Domain.ValueObjects;

namespace ProjectService.Domain.Enums;

/// <summary>
/// Приоритет задачи — Value Object.
/// </summary>
public sealed class TaskPriority : ValueObject<TaskPriority>
{
    /// <summary>
    /// Значение приоритета для хранения в БД.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Название приоритета.
    /// </summary>
    public string Name { get; }

    private TaskPriority(int value, string name)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Низкий приоритет.
    /// </summary>
    public static TaskPriority Low { get; } = new(1, "Low");

    /// <summary>
    /// Средний приоритет.
    /// </summary>
    public static TaskPriority Medium { get; } = new(2, "Medium");

    /// <summary>
    /// Высокий приоритет.
    /// </summary>
    public static TaskPriority High { get; } = new(3, "High");

    /// <summary>
    /// Критический приоритет.
    /// </summary>
    public static TaskPriority Critical { get; } = new(4, "Critical");

    /// <summary>
    /// Создаёт TaskPriority из целочисленного значения.
    /// </summary>
    public static TaskPriority FromValue(int value) =>
        value switch
        {
            1 => Low,
            2 => Medium,
            3 => High,
            4 => Critical,
            _ => throw new ArgumentException($"Unknown TaskPriority value: {value}", nameof(value))
        };

    /// <summary>
    /// Создаёт TaskPriority из названия.
    /// </summary>
    public static TaskPriority FromName(string name) =>
        name.ToLowerInvariant() switch
        {
            "low" => Low,
            "medium" => Medium,
            "high" => High,
            "critical" => Critical,
            _ => throw new ArgumentException($"Unknown TaskPriority name: {name}", nameof(name))
        };

    /// <summary>
    /// Позволяет неявно преобразовать int в TaskPriority.
    /// </summary>
    public static implicit operator TaskPriority(int value) => FromValue(value);

    /// <summary>
    /// Позволяет неявно преобразовать TaskPriority в int.
    /// </summary>
    public static implicit operator int(TaskPriority priority) => priority.Value;

    /// <summary>
    /// Получает компоненты для сравнения Value Object.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Name;
    }

    /// <summary>
    /// Возвращает строковое представление приоритета.
    /// </summary>
    public override string ToString() => Name;
}
