using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Projects.Queries;
using ProjectService.Application.Features.Tasks.Commands;
using ProjectService.Application.Features.Tasks.Queries;

namespace ProjectService.API.Controllers;

/// <summary>
/// API контроллер для управления задачами.
/// Предоставляет CRUD-операции через CQRS паттерн.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TasksController> _logger;

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="mediator">MediatR для отправки команд/запросов</param>
    /// <param name="logger">Логгер</param>
    public TasksController(IMediator mediator, ILogger<TasksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// POST api/tasks — создаёт новую задачу в проекте.
    /// </summary>
    /// <param name="command">Данные создаваемой задачи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданная задача</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание задачи для проекта: {ProjectId}", command.ProjectId);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// GET api/tasks — возвращает список всех задач.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список задач</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        // Возвращаем все задачи — в реальном проекте добавить пагинацию
        var allProjects = await _mediator.Send(new GetAllProjectsQuery(0, 1000), cancellationToken);
        var tasks = allProjects.SelectMany(p => p.Tasks).ToList();
        return Ok(tasks);
    }

    /// <summary>
    /// GET api/tasks/{id} — возвращает задачу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Задача</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET api/tasks/by-project/{projectId} — возвращает все задачи проекта.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список задач проекта</returns>
    [HttpGet("by-project/{projectId:guid}")]
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProject(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTasksByProjectQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// PUT api/tasks/{id} — обновляет задачу.
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="command">Новые данные</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>NoContent при успехе</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        command = command with { Id = id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// DELETE api/tasks/{id} — удаляет задачу.
    /// </summary>
    /// <param name="id">Идентификатор задачи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>NoContent при успехе</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }
}
