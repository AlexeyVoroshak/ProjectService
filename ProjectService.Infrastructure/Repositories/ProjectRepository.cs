using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Data;

namespace ProjectService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория проектов на основе EF Core.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Конструктор с внедрением зависимости DbContext.
    /// </summary>
    /// <param name="context">Контекст EF Core</param>
    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получает проект по идентификатору с загрузкой связанных задач.
    /// </summary>
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Получает все проекты с пагинацией и загрузкой задач.
    /// </summary>
    public async Task<IEnumerable<Project>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Добавляет новый проект в контекст.
    /// Проект будет сохранён при вызове SaveChangesAsync.
    /// </summary>
    public void Add(Project project)
    {
        _context.Projects.Add(project);
    }

    /// <summary>
    /// Обновляет существующий проект в контексте.
    /// </summary>
    public void Update(Project project)
    {
        _context.Projects.Update(project);
    }

    /// <summary>
    /// Помечает проект на удаление в контексте.
    /// </summary>
    public void Remove(Guid id)
    {
        var project = new Project();
        project.Id = id;
        _context.Projects.Attach(project);
        _context.Projects.Remove(project);
    }
}
