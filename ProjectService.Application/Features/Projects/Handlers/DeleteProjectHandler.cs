using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Domain.Exceptions;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Projects.Handlers;

/// <summary>
/// Обработчик команды удаления проекта.
/// Выполняет мягкое удаление (устанавливает статус Deleted).
/// </summary>
public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<DeleteProjectHandler> _logger;

    public DeleteProjectHandler(IProjectRepository projectRepository, ILogger<DeleteProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Удаление проекта: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Проект с ID {request.Id} не найден");

        project.Delete();

        return Unit.Value;
    }
}
