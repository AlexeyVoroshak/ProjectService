using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProjectService.Application.Behaviors;

namespace ProjectService.Application;

/// <summary>
/// Класс для регистрации сервисов Application layer в DI контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все сервисы Application layer.
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Ссылка на IServiceCollection для цепочки вызовов</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Регистрируем MediatR — все обработчики команд/запросов и pipeline behavior
        services.AddMediatR(cfg =>
        {
            // Указываем сборку, в которой находятся handlers
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Сохраняем все изменения (сущности + Outbox сообщения) после выполнения handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(SaveChangesPipelineBehavior<,>));

            // Регистрируем pipeline behavior для перехвата доменных событий (ДО SaveChanges)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(DomainEventPublisherBehavior<,>));
        });

        // Регистрируем FluentValidation — валидаторы для команд
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
