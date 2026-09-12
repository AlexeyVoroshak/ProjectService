using MediatR;
using Microsoft.Extensions.Logging;
using ProjectService.Application.DTOs;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Application.Features.Projects.Queries;
using ProjectService.Application.Services;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Enums;
using ProjectService.Domain.Repositories;
using System.Net.Sockets;
using System.Xml.Linq;

namespace ProjectService.Application.Features.Projects.Handlers;

/// <summary>
/// Обработчик команды создания проекта.
/// Создает новый проект и сохраняет его в репозиторий.
/// Доменные события будут опубликованы через Outbox pipeline behavior.
/// </summary>
public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<CreateProjectHandler> _logger;

    /// <summary>
    /// Конструктор с внедрением зависимостей.
    /// </summary>
    /// <param name="projectRepository">Репозиторий проектов</param>
    /// <param name="cacheService">Сервис кэша</param>
    /// <param name="logger">Логгер</param>
    public CreateProjectHandler(IProjectRepository projectRepository, ICacheService? cacheService, ILogger<CreateProjectHandler> logger)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает команду создания проекта.
    /// </summary>
    /// <param name="request">Запрос с данными проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданный проект в формате DTO</returns>
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Создание нового проекта: {Name}", request.Name);

        var project = new Project(request.Name, request.Description);

        _projectRepository.Add(project);

        // Инвалидируем кэш списка проектов
        if (_cacheService != null)
        {
            await _cacheService.RemoveByPatternAsync("projects:*");
        }

        // Важно: SaveChanges вызывается в ApplicationDBContext.SaveChangesAsync
        // через pipeline behavior, который также сохраняет Outbox сообщения

        return ProjectDto.FromEntity(project);
    }
}