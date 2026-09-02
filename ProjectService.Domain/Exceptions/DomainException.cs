namespace ProjectService.Domain.Exceptions;

/// <summary>
/// Базовый класс для всех доменных исключений.
/// Используется для обозначения ошибок бизнес-логики.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Создаёт новое доменное исключение с указанным сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public DomainException(string message) : base(message) { }

    /// <summary>
    /// Создаёт доменное исключение с сообщением и внутренней ошибкой.
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутренняя исключительная ситуация</param>
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
