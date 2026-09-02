using MediatR;
using ProjectService.Application.DTOs;

namespace ProjectService.Application.Features.Tasks.Queries;

/// <summary>
/// Запрос получения задачи по идентификатору.
/// </summary>
public record GetTaskByIdQuery(Guid Id) : IRequest<TaskDto>;

/// <summary>
/// Запрос получения всех задач проекта.
/// </summary>
public record GetTasksByProjectQuery(Guid ProjectId) : IRequest<List<TaskDto>>;
