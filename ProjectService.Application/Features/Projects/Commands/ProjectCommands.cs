using MediatR;
using ProjectService.Application.DTOs;

namespace ProjectService.Application.Features.Projects.Commands;

/// <summary>
/// Команда создания нового проекта.
/// </summary>
public record CreateProjectCommand(string Name, string? Description = null) : IRequest<ProjectDto>;

/// <summary>
/// Команда обновления деталей проекта.
/// </summary>
public record UpdateProjectCommand(Guid Id, string Name, string? Description = null) : IRequest<Unit>;

/// <summary>
/// Команда удаления проекта.
/// </summary>
public record DeleteProjectCommand(Guid Id) : IRequest<Unit>;
