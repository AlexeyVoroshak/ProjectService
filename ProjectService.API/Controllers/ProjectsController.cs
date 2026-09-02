using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Application.Features.Projects.Queries;
using ProjectService.Application.Features.Tasks.Commands;
using ProjectService.Application.Features.Tasks.Queries;

namespace ProjectService.API.Controllers;

/// <summary>
/// API контроллер для управления проектами.
/// Предоставляет CRUD-операции через CQRS паттерн.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectsController> _logger;

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="mediator">MediatR для отправки команд/запросов</param>
    /// <param name="logger">Логгер</param>
    public ProjectsController(IMediator mediator, ILogger<ProjectsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// POST api/projects — создаёт новый проект.
    /// </summary>
    /// <param name="command">Данные создаваемого проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданный проект</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание проекта: {Name}", command.Name);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// GET api/projects — возвращает список всех проектов с пагинацией.
    /// </summary>
    /// <param name="skip">Количество пропускаемых записей</param>
    /// <param name="take">Количество записей для возврата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список проектов</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllProjectsQuery(skip, take), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET api/projects/{id} — возвращает проект по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Проект</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// PUT api/projects/{id} — обновляет проект.
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="command">Новые данные</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>NoContent при успехе</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        command = command with { Id = id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// DELETE api/projects/{id} — удаляет проект (мягкое удаление).
    /// </summary>
    /// <param name="id">Идентификатор проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>NoContent при успехе</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }
}
