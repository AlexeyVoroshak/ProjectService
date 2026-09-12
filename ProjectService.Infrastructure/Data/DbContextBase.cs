using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Entities;

namespace ProjectService.Infrastructure.Data;

/// <summary>
/// Базовый контекст EF Core для работы с БД.
/// Содержит DbSet для сущностей доменной модели и Outbox сообщений.
/// Переопределяется в ApplicationDbContext для регистрации конкретных DbSet.
/// </summary>
public abstract class DbContextBase : DbContext
{
    /// <summary>
    /// Конструктор с параметром конфигурации.
    /// </summary>
    /// <param name="options">Настройки контекста</param>
    protected DbContextBase(DbContextOptions options) : base(options) { }

    /// <summary>
    /// Набор сущностей OutboxMessage для управления сообщениями Outbox.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    /// <summary>
    /// Переопределяется в производном классе для регистрации DbSet сущностей.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Сохраняет все изменения в БД.
    /// В рамках этой транзакции также сохраняются Outbox сообщения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество сохранённых элементов</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
