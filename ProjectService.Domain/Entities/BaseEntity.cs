using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProjectService.Infrastructure")]

namespace ProjectService.Domain.Entities;

/// <summary>
/// Базовый класс для всех сущностей доменной модели.
/// Определяет общие свойства: идентификатор, даты создания и обновления.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Уникальный идентификатор сущности (GUID).
    /// </summary>
    public Guid Id { get; protected internal set; }

    /// <summary>
    /// Дата и время создания записи.
    /// </summary>
    public DateTime CreatedAt { get; protected internal set; }

    /// <summary>
    /// Дата и время последнего обновления.
    /// </summary>
    public DateTime UpdatedAt { get; protected internal set; }

    /// <summary>
    /// Метод для установки начальных значений при создании сущности.
    /// Вызывается один раз в конструкторе.
    /// </summary>
    protected void SetInitialValues()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Метод для обновления метки времени при изменении сущности.
    /// </summary>
    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
