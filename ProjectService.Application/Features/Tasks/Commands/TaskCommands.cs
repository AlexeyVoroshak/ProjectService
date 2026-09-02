using MediatR;
using ProjectService.Application.DTOs;
using DomainEnums = ProjectService.Domain.Enums;

namespace ProjectService.Application.Features.Tasks.Commands;

/// <summary>
/// Команда создания новой задачи.
/// </summary>
public record CreateTaskCommand(Guid ProjectId, string Title, DomainEnums.TaskPriority Priority, string? Description = null) : IRequest<TaskDto>;

/// <summary>
/// Команда обновления деталей задачи.
/// </summary>
public record UpdateTaskCommand(Guid Id, string Title, string? Description = null) : IRequest<Unit>;

/// <summary>
/// Команда удаления задачи.
/// </summary>
public record DeleteTaskCommand(Guid Id) : IRequest<Unit>;

/// <summary>
/// Команда изменения статуса задачи.
/// </summary>
public record UpdateTaskStatusCommand(Guid Id, DomainEnums.TaskStatus Status) : IRequest<Unit>;
