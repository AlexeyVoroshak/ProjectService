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
    /// Очищает доменные события после публикации.
    /// Переопределяется в наследниках для очистки коллекции событий.
    /// </summary>
    public abstract void ClearDomainEvents();

    /// <summary>
    /// Уникальный идентификатор сущности (GUID).
    /// </summary>
    public Guid Id { get; protected internal set; }

    /// <summary>
    /// Дата и время создания записи.
    /// </summary>
    public DateTime CreatedAt { get; protected internal set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата и время последнего обновления.
    /// </summary>
    public DateTime UpdatedAt { get; protected internal set; } = DateTime.UtcNow;
}
