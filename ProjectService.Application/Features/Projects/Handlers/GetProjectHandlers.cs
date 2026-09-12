using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Projects.Queries;
using ProjectService.Application.Services;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Projects.Handlers;

/// <summary>
/// Обработчик запроса получения проекта по идентификатору.
/// </summary>
public class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<GetProjectByIdHandler> _logger;

    public GetProjectByIdHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<GetProjectByIdHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение проекта: {Id}", request.Id);

        var cacheKey = $"project:{request.Id}";

        // Пытаемся получить из кэша
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<ProjectDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Проект получен из кэша: {Id}", request.Id);
                return cached;
            }
        }

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.Id} не найден");

        var result = ProjectDto.FromEntity(project);

        // Сохраняем в кэш на 10 минут
        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, result, 10);
        }

        return result;
    }
}

/// <summary>
/// Обработчик запроса получения списка всех проектов.
/// </summary>
public class GetAllProjectsHandler : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<GetAllProjectsHandler> _logger;

    public GetAllProjectsHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<GetAllProjectsHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение списка проектов: Skip={Skip}, Take={Take}", request.Skip, request.Take);

        var cacheKey = $"projects:all:{request.Skip}:{request.Take}";

        // Пытаемся получить из кэша
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<List<ProjectDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Список проектов получен из кэша");
                return cached;
            }
        }

        var projects = await _projectRepository.GetAllAsync(request.Skip, request.Take, cancellationToken);
        var result = projects.Select(ProjectDto.FromEntity).ToList();

        // Сохраняем в кэш на 5 минут
        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, result, 5);
        }

        return result;
    }
}
