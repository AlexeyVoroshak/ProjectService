using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Domain.Exceptions;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Features.Projects.Handlers;

/// <summary>
/// Обработчик команды обновления проекта.
/// </summary>
public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<UpdateProjectHandler> _logger;

    public UpdateProjectHandler(IProjectRepository projectRepository, ILogger<UpdateProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Обновление проекта: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Проект с ID {request.Id} не найден");

        project.UpdateDetails(request.Name, request.Description);

        return Unit.Value;
    }
}
