using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Repositories;

namespace ProjectService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория Outbox на основе EF Core.
/// Обеспечивает атомарное сохранение и выборку неподтверждённых сообщений.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Конструктор с внедрением зависимости IUnitOfWork.
    /// </summary>
    /// <param name="unitOfWork">Unit of Work</param>
    public OutboxRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Сохраняет одно сообщение Outbox в БД.
    /// Вызывается в рамках той же транзакции, что и доменная операция.
    /// </summary>
    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Set<OutboxMessage>().AddAsync(message, cancellationToken);
    }

    /// <summary>
    /// Получает пакет неподтверждённых сообщений для публикации.
    /// Сортирует по дате создания для обработки в порядке FIFO.
    /// </summary>
    public async Task<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Помечает сообщения как опубликованные.
    /// Устанавливает статус Published и дату публикации.
    /// </summary>
    public async Task MarkAsPublishedAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        var messages = await _unitOfWork.Set<OutboxMessage>()
            .Where(m => messageIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = OutboxMessageStatus.Published;
            message.PublishedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Помечает сообщение как failed с описанием ошибки.
    /// Увеличивает счётчик попыток.
    /// </summary>
    public async Task MarkAsFailedAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var message = await _unitOfWork.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.LastError = errorMessage;
            message.FailedAttempts++;
        }
    }
}
