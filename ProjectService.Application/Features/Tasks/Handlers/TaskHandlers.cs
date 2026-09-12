using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Tasks.Commands;
using ProjectService.Application.Features.Tasks.Queries;
using ProjectService.Application.Services;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Tasks.Handlers;

/// <summary>
/// Обработчик команды создания задачи.
/// Задача добавляется в указанный проект.
/// </summary>
public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<CreateTaskHandler> _logger;

    public CreateTaskHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<CreateTaskHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Создание задачи для проекта: {ProjectId}", request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден");

        project.AddTask(request.Title, request.Priority, request.Description);

        // Инвалидируем кэш задач проекта
        if (_cacheService != null)
        {
            await _cacheService.RemoveByPatternAsync($"tasks:project:{request.ProjectId}");
        }

        return TaskDto.FromEntity(project.Tasks.Last());
    }
}

/// <summary>
/// Обработчик команды обновления задачи.
/// </summary>
public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<UpdateTaskHandler> _logger;

    public UpdateTaskHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<UpdateTaskHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Обновление задачи: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена");

        var task = project.Tasks.FirstOrDefault(t => t.Id == request.Id)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена в проекте");

        task.UpdateDetails(request.Title, request.Description);

        // Инвалидируем кэш
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"task:{request.Id}");
            await _cacheService.RemoveByPatternAsync($"tasks:project:{project.Id}");
        }

        return Unit.Value;
    }
}

/// <summary>
/// Обработчик команды удаления задачи.
/// </summary>
public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<DeleteTaskHandler> _logger;

    public DeleteTaskHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<DeleteTaskHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Удаление задачи: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена");

        var task = project.Tasks.FirstOrDefault(t => t.Id == request.Id)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена");

        ((List<Domain.Entities.TaskEntity>)project.Tasks).Remove(task);

        // Инвалидируем кэш
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"task:{request.Id}");
            await _cacheService.RemoveByPatternAsync($"tasks:project:{project.Id}");
        }

        return Unit.Value;
    }
}

/// <summary>
/// Обработчик запроса получения задачи по идентификатору.
/// </summary>
public class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<GetTaskByIdHandler> _logger;

    public GetTaskByIdHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<GetTaskByIdHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение задачи: {Id}", request.Id);

        var cacheKey = $"task:{request.Id}";

        // Пытаемся получить из кэша
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<TaskDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Задача получена из кэша: {Id}", request.Id);
                return cached;
            }
        }

        var projects = await _projectRepository.GetAllAsync(cancellationToken: cancellationToken);
        var task = projects.SelectMany(p => p.Tasks).FirstOrDefault(t => t.Id == request.Id)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена");

        var result = TaskDto.FromEntity(task);

        // Сохраняем в кэш на 10 минут
        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, result, 10);
        }

        return result;
    }
}

/// <summary>
/// Обработчик запроса получения всех задач проекта.
/// </summary>
public class GetTasksByProjectHandler : IRequestHandler<GetTasksByProjectQuery, List<TaskDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<GetTasksByProjectHandler> _logger;

    public GetTasksByProjectHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<GetTasksByProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение задач проекта: {ProjectId}", request.ProjectId);

        var cacheKey = $"tasks:project:{request.ProjectId}";

        // Пытаемся получить из кэша
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetAsync<List<TaskDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Задачи проекта получены из кэша: {ProjectId}", request.ProjectId);
                return cached;
            }
        }

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден");

        var result = project.Tasks.Select(TaskDto.FromEntity).ToList();

        // Сохраняем в кэш на 5 минут
        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, result, 5);
        }

        return result;
    }
}
