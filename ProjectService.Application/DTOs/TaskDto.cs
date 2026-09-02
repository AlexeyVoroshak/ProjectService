using DomainEnums = ProjectService.Domain.Enums;

namespace ProjectService.Application.DTOs;

/// <summary>
/// DTO для представления задачи.
/// Используется для передачи данных между слоями и API.
/// </summary>
public record TaskDto
{
    /// <summary>
    /// Идентификатор задачи.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор родительского проекта.
    /// </summary>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// Название задачи.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Статус задачи.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// Приоритет задачи.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Дата и время создания.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата и время последнего обновления.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Преобразует сущность TaskEntity в DTO.
    /// </summary>
    /// <param name="task">Сущность задачи</param>
    /// <returns>Экземпляр TaskDto</returns>
    public static TaskDto FromEntity(ProjectService.Domain.Entities.TaskEntity task)
    {
        return new TaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
