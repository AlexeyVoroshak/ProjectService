using Microsoft.EntityFrameworkCore;
using ProjectService.Application.Services;
using ProjectService.Domain.Repositories;

namespace ProjectService.Infrastructure;

/// <summary>
/// Реализация Unit of Work на основе ApplicationDbContext.
/// Управляет транзакциями и сохранением изменений в БД.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получает DbSet для работы с сущностями типа T.
    /// </summary>
    public DbSet<T> Set<T>() where T : class
        => _context.Set<T>();

    /// <summary>
    /// Сохраняет все изменения в БД.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Начинает новую транзакцию.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _transaction;
    }

    /// <summary>
    /// Коммитит текущую транзакцию и сохраняет изменения.
    /// При ошибке выполняет откат.
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Откатывает текущую транзакцию.
    /// </summary>
    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
    }

    /// <summary>
    /// Освобождает ресурсы транзакции.
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
