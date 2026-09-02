using ProjectService.Domain.ValueObjects;

namespace ProjectService.Domain.Enums;

/// <summary>
/// Статус задачи — Value Object.
/// </summary>
public sealed class TaskStatus : ValueObject<TaskStatus>
{
    /// <summary>
    /// Значение статуса для хранения в БД.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Название статуса.
    /// </summary>
    public string Name { get; }

    private TaskStatus(int value, string name)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Задача не начата.
    /// </summary>
    public static TaskStatus Todo { get; } = new(1, "Todo");

    /// <summary>
    /// Задача в процессе выполнения.
    /// </summary>
    public static TaskStatus InProgress { get; } = new(2, "InProgress");

    /// <summary>
    /// Задача завершена.
    /// </summary>
    public static TaskStatus Done { get; } = new(3, "Done");

    /// <summary>
    /// Задача отменена.
    /// </summary>
    public static TaskStatus Cancelled { get; } = new(4, "Cancelled");

    /// <summary>
    /// Создаёт TaskStatus из целочисленного значения.
    /// </summary>
    public static TaskStatus FromValue(int value) =>
        value switch
        {
            1 => Todo,
            2 => InProgress,
            3 => Done,
            4 => Cancelled,
            _ => throw new ArgumentException($"Unknown TaskStatus value: {value}", nameof(value))
        };

    /// <summary>
    /// Создаёт TaskStatus из названия.
    /// </summary>
    public static TaskStatus FromName(string name) =>
        name.ToLowerInvariant() switch
        {
            "todo" => Todo,
            "inprogress" => InProgress,
            "done" => Done,
            "cancelled" => Cancelled,
            _ => throw new ArgumentException($"Unknown TaskStatus name: {name}", nameof(name))
        };

    /// <summary>
    /// Позволяет неявно преобразовать int в TaskStatus.
    /// </summary>
    public static implicit operator TaskStatus(int value) => FromValue(value);

    /// <summary>
    /// Позволяет неявно преобразовать TaskStatus в int.
    /// </summary>
    public static implicit operator int(TaskStatus status) => status.Value;

    /// <summary>
    /// Получает компоненты для сравнения Value Object.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Name;
    }

    /// <summary>
    /// Возвращает строковое представление статуса.
    /// </summary>
    public override string ToString() => Name;
}
