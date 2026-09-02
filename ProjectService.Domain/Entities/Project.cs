using ProjectService.Domain.Enums;
using ProjectService.Domain.Events;

namespace ProjectService.Domain.Entities;

/// <summary>
/// Агрегатный корень — проект.
/// Управляет жизненным циклом проекта и содержит коллекцию задач.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Название проекта.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Описание проекта.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Текущий статус проекта.
    /// </summary>
    public ProjectStatus Status { get; private set; }

    /// <summary>
    /// Коллекция задач проекта.
    /// </summary>
    private readonly List<TaskEntity> _tasks = [];
    public IReadOnlyCollection<TaskEntity> Tasks => _tasks.AsReadOnly();

    /// <summary>
    /// Публичный конструктор для EF Core.
    /// </summary>
    public Project()
    {
        SetInitialValues();
        Status = ProjectStatus.Active;
        Name = string.Empty;
    }

    /// <summary>
    /// Создаёт новый проект с указанными параметрами.
    /// </summary>
    /// <param name="name">Название проекта</param>
    /// <param name="description">Описание проекта</param>
    public Project(string name, string? description = null)
    {
        SetInitialValues();
        Name = name;
        Description = description;
        Status = ProjectStatus.Active;
        
        // Добавляем доменное событие создания проекта
        AddDomainEvent(new ProjectCreatedEvent(this));
    }

    /// <summary>
    /// Обновляет название и описание проекта.
    /// </summary>
    /// <param name="name">Новое название</param>
    /// <param name="description">Новое описание</param>
    public void UpdateDetails(string name, string? description = null)
    {
        Name = name;
        Description = description;
        UpdateTimestamp();
    }

    /// <summary>
    /// Архивирует проект.
    /// </summary>
    public void Archive()
    {
        Status = ProjectStatus.Archived;
        UpdateTimestamp();
        AddDomainEvent(new ProjectArchivedEvent(this));
    }

    /// <summary>
    /// Восстанавливает проект из архива.
    /// </summary>
    public void Activate()
    {
        Status = ProjectStatus.Active;
        UpdateTimestamp();
    }

    /// <summary>
    /// Удаляет проект.
    /// </summary>
    public void Delete()
    {
        Status = ProjectStatus.Deleted;
        UpdateTimestamp();
        AddDomainEvent(new ProjectDeletedEvent(this));
    }

    /// <summary>
    /// Добавляет новую задачу в проект.
    /// </summary>
    /// <param name="title">Название задачи</param>
    /// <param name="description">Описание задачи</param>
    /// <param name="priority">Приоритет задачи</param>
    public void AddTask(string title, string? description = null, TaskPriority priority = TaskPriority.Medium)
    {
        var task = new TaskEntity(this, title, description, priority);
        _tasks.Add(task);
    }

    /// <summary>
    /// Список доменных событий, связанных с данной сущностью.
    /// Используется для последующей публикации через Outbox.
    /// </summary>
    private readonly List<object> _domainEvents = [];
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Добавляет доменное событие.
    /// </summary>
    private void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Очищает список доменных событий после обработки.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
