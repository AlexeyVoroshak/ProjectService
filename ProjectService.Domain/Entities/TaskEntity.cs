using ProjectService.Domain.Enums;
using ProjectService.Domain.Events;

namespace ProjectService.Domain.Entities;

/// <summary>
/// Сущность задачи внутри агрегата Project.
/// Не является агрегатным корнем — управляется через Project.
/// </summary>
public class TaskEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор проекта, которому принадлежит задача.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Ссылка на родительский проект.
    /// </summary>
    public Project Project { get; private set; } = null!;

    /// <summary>
    /// Название задачи.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Текущий статус задачи.
    /// </summary>
    public Enums.TaskStatus Status { get; private set; }

    /// <summary>
    /// Приоритет задачи.
    /// </summary>
    public TaskPriority Priority { get; private set; }

    /// <summary>
    /// Публичный конструктор для EF Core.
    /// </summary>
    public TaskEntity()
    {
        SetInitialValues();
        Title = string.Empty;
        Status = Enums.TaskStatus.Todo;
        Priority = TaskPriority.Medium;
    }

    /// <summary>
    /// Создаёт новую задачу в рамках указанного проекта.
    /// </summary>
    /// <param name="project">Родительский проект</param>
    /// <param name="title">Название задачи</param>
    /// <param name="description">Описание задачи</param>
    /// <param name="priority">Приоритет задачи</param>
    public TaskEntity(Project project, string title, string? description, TaskPriority priority)
    {
        SetInitialValues();
        ProjectId = project.Id;
        Project = project;
        Title = title;
        Description = description;
        Status = Enums.TaskStatus.Todo;
        Priority = priority;
        
        // Добавляем доменное событие создания задачи
        AddDomainEvent(new TaskCreatedEvent(this));
    }

    /// <summary>
    /// Обновляет название и описание задачи.
    /// </summary>
    /// <param name="title">Новое название</param>
    /// <param name="description">Новое описание</param>
    public void UpdateDetails(string title, string? description = null)
    {
        Title = title;
        Description = description;
        UpdateTimestamp();
    }

    /// <summary>
    /// Изменяет приоритет задачи.
    /// </summary>
    /// <param name="priority">Новый приоритет</param>
    public void UpdatePriority(TaskPriority priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    /// <summary>
    /// Переводит задачу в статус «в процессе».
    /// </summary>
    public void Start()
    {
        Status = Enums.TaskStatus.InProgress;
        UpdateTimestamp();
    }

    /// <summary>
    /// Завершает задачу.
    /// </summary>
    public void Complete()
    {
        Status = Enums.TaskStatus.Done;
        UpdateTimestamp();
        AddDomainEvent(new TaskCompletedEvent(this));
    }

    /// <summary>
    /// Отменяет задачу.
    /// </summary>
    public void Cancel()
    {
        Status = Enums.TaskStatus.Cancelled;
        UpdateTimestamp();
    }

    /// <summary>
    /// Возвращает задачу в статус «к выполнению».
    /// </summary>
    public void Reset()
    {
        Status = Enums.TaskStatus.Todo;
        UpdateTimestamp();
    }

    /// <summary>
    /// Список доменных событий.
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
    public override void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
