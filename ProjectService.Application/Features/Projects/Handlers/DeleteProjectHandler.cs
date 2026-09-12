using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Application.Services;
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
    private readonly ICacheService? _cacheService;
    private readonly ILogger<DeleteProjectHandler> _logger;

    public DeleteProjectHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<DeleteProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Удаление проекта: {Id}", request.Id);

        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Проект с ID {request.Id} не найден");

        project.Delete();

        // Инвалидируем кэш
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"project:{request.Id}");
            await _cacheService.RemoveByPatternAsync("projects:*");
        }

        return Unit.Value;
    }
}
