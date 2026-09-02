using MediatR;
using ProjectService.Application.DTOs;

namespace ProjectService.Application.Features.Projects.Queries;

/// <summary>
/// Запрос получения проекта по идентификатору.
/// </summary>
public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto>;

/// <summary>
/// Запрос получения списка всех проектов с пагинацией.
/// </summary>
public record GetAllProjectsQuery(int Skip = 0, int Take = 50) : IRequest<List<ProjectDto>>;
