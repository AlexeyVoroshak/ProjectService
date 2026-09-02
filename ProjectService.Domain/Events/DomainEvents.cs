using ProjectService.Domain.Entities;

namespace ProjectService.Domain.Events;

/// <summary>
/// Событие создания проекта.
/// Публикуется в Outbox после успешного сохранения проекта в БД.
/// </summary>
public record ProjectCreatedEvent(Project Project) : IDomainEvent;

/// <summary>
/// Событие обновления проекта.
/// </summary>
public record ProjectUpdatedEvent(Project Project) : IDomainEvent;

/// <summary>
/// Событие архивирования проекта.
/// </summary>
public record ProjectArchivedEvent(Project Project) : IDomainEvent;

/// <summary>
/// Событие удаления проекта.
/// </summary>
public record ProjectDeletedEvent(Project Project) : IDomainEvent;

/// <summary>
/// Событие создания задачи.
/// </summary>
public record TaskCreatedEvent(TaskEntity Task) : IDomainEvent;

/// <summary>
/// Событие завершения задачи.
/// </summary>
public record TaskCompletedEvent(TaskEntity Task) : IDomainEvent;
