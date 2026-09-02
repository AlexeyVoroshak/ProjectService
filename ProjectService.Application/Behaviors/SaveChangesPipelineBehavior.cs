using MediatR;
using ProjectService.Application.Services;
using ProjectService.Domain.Repositories;

namespace ProjectService.Application.Behaviors;

/// <summary>
/// Pipeline behavior для вызова SaveChangesAsync после выполнения handler.
/// 
/// Гарантирует, что все изменения сущностей (включая Outbox сообщения,
/// добавленные DomainEventPublisherBehavior) сохраняются в БД в рамках
/// одной транзакции.
/// 
/// Должен быть зарегистрирован ПОСЛЕ DomainEventPublisherBehavior,
/// чтобы Outbox сообщения были добавлены до вызова SaveChanges.
/// </summary>
public class SaveChangesPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveChangesPipelineBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
