namespace ProjectService.Domain.Entities;

/// <summary>
/// Сообщение в Outbox — гарантированная доставка доменных событий в Kafka.
/// Записывается в БД в той же транзакции, что и доменная операция.
/// Background worker периодически опрашивает неподтверждённые сообщения и публикует их.
/// </summary>
public class OutboxMessage : BaseEntity
{
    /// <summary>
    /// Идентификатор агрегата, вызвавшего событие.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Полное имя типа события (например, "ProjectService.Domain.Events.ProjectCreatedEvent").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Сериализованное в JSON тело события.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Статус обработки сообщения.
    /// </summary>
    public OutboxMessageStatus Status { get; set; }

    /// <summary>
    /// Дата и время публикации в Kafka (null, если ещё не опубликовано).
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Количество попыток публикации.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Описание последней ошибки (если публикация не удалась).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Версия для оптимистичной блокировки.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Создаёт новое сообщение Outbox.
    /// </summary>
    /// <param name="aggregateId">Идентификатор агрегата</param>
    /// <param name="eventType">Имя типа события</param>
    /// <param name="payload">JSON-сериализованное событие</param>
    /// <returns>Экземпляр OutboxMessage</returns>
    public static OutboxMessage Create(string aggregateId, string eventType, string payload)
    {
        return new OutboxMessage
        {
            AggregateId = aggregateId,
            EventType = eventType,
            Payload = payload,
            Status = OutboxMessageStatus.Pending
        };
    }

    public override void ClearDomainEvents()
    {
        // OutboxMessage не имеет доменных событий, поэтому метод оставлен пустым.
    }
}

/// <summary>
/// Статус обработки сообщения Outbox.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Сообщение ожидает публикации.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Сообщение успешно опубликовано в Kafka.
    /// </summary>
    Published = 2,

    /// <summary>
    /// При публикации произошла ошибка.
    /// </summary>
    Failed = 3
}
