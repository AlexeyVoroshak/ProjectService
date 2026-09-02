using ProjectService.Domain.ValueObjects;

namespace ProjectService.Domain.Enums;

/// <summary>
/// Статус проекта — Value Object.
/// </summary>
public sealed class ProjectStatus : ValueObject<ProjectStatus>
{
    /// <summary>
    /// Значение статуса для хранения в БД.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Название статуса.
    /// </summary>
    public string Name { get; }

    private ProjectStatus(int value, string name)
    {
        Value = value;
        Name = name;
    }

    /// <summary>
    /// Проект активен.
    /// </summary>
    public static ProjectStatus Active { get; } = new(1, "Active");

    /// <summary>
    /// Проект архивирован.
    /// </summary>
    public static ProjectStatus Archived { get; } = new(2, "Archived");

    /// <summary>
    /// Проект удалён.
    /// </summary>
    public static ProjectStatus Deleted { get; } = new(3, "Deleted");

    /// <summary>
    /// Создаёт ProjectStatus из целочисленного значения.
    /// </summary>
    public static ProjectStatus FromValue(int value) =>
        value switch
        {
            1 => Active,
            2 => Archived,
            3 => Deleted,
            _ => throw new ArgumentException($"Unknown ProjectStatus value: {value}", nameof(value))
        };

    /// <summary>
    /// Создаёт ProjectStatus из названия.
    /// </summary>
    public static ProjectStatus FromName(string name) =>
        name.ToLowerInvariant() switch
        {
            "active" => Active,
            "archived" => Archived,
            "deleted" => Deleted,
            _ => throw new ArgumentException($"Unknown ProjectStatus name: {name}", nameof(name))
        };

    /// <summary>
    /// Позволяет неявно преобразовать int в ProjectStatus.
    /// </summary>
    public static implicit operator ProjectStatus(int value) => FromValue(value);

    /// <summary>
    /// Позволяет неявно преобразовать ProjectStatus в int.
    /// </summary>
    public static implicit operator int(ProjectStatus status) => status.Value;

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
