namespace ProjectService.Domain.Events;

/// <summary>
/// Базовый интерфейс для всех доменных событий.
/// Используется MediatR для публикации событий через pipeline behavior.
/// </summary>
public interface IDomainEvent;
