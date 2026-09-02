using ProjectService.Domain.Entities;

namespace ProjectService.Domain.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с сообщениями Outbox.
/// Обеспечивает атомарное сохранение и выборку неподтверждённых сообщений.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Сохраняет одно сообщение Outbox в БД.
    /// Вызывается в рамках той же транзакции, что и доменная операция.
    /// </summary>
    /// <param name="message">Сообщение для сохранения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает пакет неподтверждённых сообщений для публикации.
    /// </summary>
    /// <param name="batchSize">Размер пакета (по умолчанию 10)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Коллекция сообщений со статусом Pending</returns>
    Task<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщения как опубликованные.
    /// </summary>
    /// <param name="messageIds">Список идентификаторов сообщений</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task MarkAsPublishedAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщение как failed с описанием ошибки.
    /// </summary>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="errorMessage">Описание ошибки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task MarkAsFailedAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken = default);
}
