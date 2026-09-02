using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Entities;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Data;

namespace ProjectService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория Outbox на основе EF Core.
/// Обеспечивает атомарное сохранение и выборку неподтверждённых сообщений.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Конструктор с внедрением зависимости DbContext.
    /// </summary>
    /// <param name="context">Контекст EF Core</param>
    public OutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Сохраняет одно сообщение Outbox в БД.
    /// Вызывается в рамках той же транзакции, что и доменная операция.
    /// </summary>
    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    /// <summary>
    /// Получает пакет неподтверждённых сообщений для публикации.
    /// Сортирует по дате создания для обработки в порядке FIFO.
    /// </summary>
    public async Task<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
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
        var messages = await _context.OutboxMessages
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
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.LastError = errorMessage;
            message.FailedAttempts++;
        }
    }
}
