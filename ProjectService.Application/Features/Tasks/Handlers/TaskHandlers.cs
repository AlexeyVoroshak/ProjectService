using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Tasks.Commands;
using ProjectService.Application.Features.Tasks.Queries;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Tasks.Handlers;

/// <summary>
/// Обработчик команды создания задачи.
/// Задача добавляется в указанный проект.
/// </summary>
public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<CreateTaskHandler> _logger;

    public CreateTaskHandler(IProjectRepository projectRepository, ILogger<CreateTaskHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Создание задачи для проекта: {ProjectId}", request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден");

        project.AddTask(request.Title, request.Priority, request.Description);

        return TaskDto.FromEntity(project.Tasks.Last());
    }
}

/// <summary>
/// Обработчик команды обновления задачи.
/// </summary>
public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<UpdateTaskHandler> _logger;

    public UpdateTaskHandler(IProjectRepository projectRepository, ILogger<UpdateTaskHandler> logger)
    {
        _projectRepository = projectRepository;
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

        return Unit.Value;
    }
}

/// <summary>
/// Обработчик команды удаления задачи.
/// </summary>
public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<DeleteTaskHandler> _logger;

    public DeleteTaskHandler(IProjectRepository projectRepository, ILogger<DeleteTaskHandler> logger)
    {
        _projectRepository = projectRepository;
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

        return Unit.Value;
    }
}

/// <summary>
/// Обработчик запроса получения задачи по идентификатору.
/// </summary>
public class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetTaskByIdHandler> _logger;

    public GetTaskByIdHandler(IProjectRepository projectRepository, ILogger<GetTaskByIdHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение задачи: {Id}", request.Id);

        var projects = await _projectRepository.GetAllAsync(cancellationToken: cancellationToken);
        var task = projects.SelectMany(p => p.Tasks).FirstOrDefault(t => t.Id == request.Id)
            ?? throw new KeyNotFoundException($"Задача с ID {request.Id} не найдена");

        return TaskDto.FromEntity(task);
    }
}

/// <summary>
/// Обработчик запроса получения всех задач проекта.
/// </summary>
public class GetTasksByProjectHandler : IRequestHandler<GetTasksByProjectQuery, List<TaskDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetTasksByProjectHandler> _logger;

    public GetTasksByProjectHandler(IProjectRepository projectRepository, ILogger<GetTasksByProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получение задач проекта: {ProjectId}", request.ProjectId);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Проект с ID {request.ProjectId} не найден");

        return project.Tasks.Select(TaskDto.FromEntity).ToList();
    }
}
