using Microsoft.EntityFrameworkCore;
using ProjectService.Application.Services;
using ProjectService.Domain.Entities;
using ProjectService.Infrastructure.Data.Configurations;

namespace ProjectService.Infrastructure.Data;

/// <summary>
/// Контекст EF Core для работы с базой данных проекта.
/// Содержит DbSet для всех сущностей доменной модели и Outbox сообщений.
/// </summary>
public class ApplicationDbContext : ApplicationDBContext
{
    /// <summary>
    /// Конструктор с параметром конфигурации.
    /// </summary>
    /// <param name="options">Настройки контекста</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /// <summary>
    /// Набор сущностей Project для CRUD-операций.
    /// </summary>
    public DbSet<Project> Projects { get; set; } = null!;

    /// <summary>
    /// Набор сущностей TaskEntity для CRUD-операций.
    /// </summary>
    public DbSet<TaskEntity> Tasks { get; set; } = null!;

    /// <summary>
    /// Настраивает маппинг сущностей и ограничения БД.
    /// </summary>
    /// <param name="modelBuilder">Builder для конфигурации модели</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем конфигурации для всех сущностей
        ProjectConfiguration.Configure(modelBuilder.Entity<Project>());
        TaskEntityConfiguration.Configure(modelBuilder.Entity<TaskEntity>());
        OutboxMessageConfiguration.Configure(modelBuilder.Entity<OutboxMessage>());
    }

    /// <summary>
    /// Переопределяем SaveChanges для автоматической установки UpdatedAt.
    /// Это гарантирует, что метка времени обновляется при любом изменении сущности.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество сохранённых элементов</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Обновляем UpdatedAt у всех изменённых сущностей
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
