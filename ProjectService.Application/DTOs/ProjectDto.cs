using ProjectService.Domain.Enums;

namespace ProjectService.Application.DTOs;

/// <summary>
/// DTO для представления проекта.
/// Используется для передачи данных между слоями и API.
/// </summary>
public record ProjectDto
{
    /// <summary>
    /// Идентификатор проекта.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название проекта.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Описание проекта.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Статус проекта.
    /// </summary>
    public ProjectStatus Status { get; init; }

    /// <summary>
    /// Дата и время создания.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата и время последнего обновления.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Список задач проекта.
    /// </summary>
    public List<TaskDto> Tasks { get; init; } = [];

    /// <summary>
    /// Преобразует сущность Project в DTO.
    /// </summary>
    /// <param name="project">Сущность проекта</param>
    /// <returns>Экземпляр ProjectDto</returns>
    public static ProjectDto FromEntity(ProjectService.Domain.Entities.Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Tasks = project.Tasks.Select(TaskDto.FromEntity).ToList()
        };
    }
}
