using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Projects.Queries;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Projects.Handlers;

/// <summary>
/// Обработчик запроса получения проекта по идентификатору.
/// </summary>
public class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetProjectByIdHandler> _logger;

    public GetProjectByIdHandler(IProjectRepository projectRepository, ILogger<GetProjectByIdHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение проекта: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.Id} не найден");

        return ProjectDto.FromEntity(project);
    }
}

/// <summary>
/// Обработчик запроса получения списка всех проектов.
/// </summary>
public class GetAllProjectsHandler : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetAllProjectsHandler> _logger;

    public GetAllProjectsHandler(IProjectRepository projectRepository, ILogger<GetAllProjectsHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение списка проектов: Skip={Skip}, Take={Take}", request.Skip, request.Take);

        var projects = await _projectRepository.GetAllAsync(request.Skip, request.Take, cancellationToken);
        return projects.Select(ProjectDto.FromEntity).ToList();
    }
}
