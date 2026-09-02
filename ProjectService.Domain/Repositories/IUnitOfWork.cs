using Microsoft.EntityFrameworkCore;

namespace ProjectService.Domain.Repositories;

/// <summary>
/// Интерфейс Unit of Work для управления транзакциями и сохранением изменений.
/// Предоставляет доступ к DbSet через generic-метод Set&lt;T&gt;().
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Получает DbSet для работы с сущностями типа T.
    /// </summary>
    DbSet<T> Set<T>() where T : class;

    /// <summary>
    /// Сохраняет все изменения в БД.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество сохранённых элементов</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Начинает новую транзакцию.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект транзакции</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Коммитит текущую транзакцию и сохраняет изменения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Откатывает текущую транзакцию.
    /// </summary>
    Task RollbackAsync();
}
