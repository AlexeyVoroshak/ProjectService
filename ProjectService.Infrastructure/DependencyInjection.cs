using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectService.Application.Services;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Data;
using ProjectService.Infrastructure.Messaging;
using ProjectService.Infrastructure.Repositories;

namespace ProjectService.Infrastructure;

/// <summary>
/// Класс для регистрации сервисов Infrastructure layer в DI контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все сервисы Infrastructure layer.
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <returns>Ссылка на IServiceCollection для цепочки вызовов</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем DbContext для PostgreSQL (Scoped)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Настраиваем таймауты команд
                npgsqlOptions.CommandTimeout(30);
                // Включаем логирование SQL-запросов (для отладки)
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        // Регистрируем Unit of Work с явным указанием зависимости
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<ApplicationDbContext>()));

        // Регистрируем репозитории (Scoped — создаются в рамках одного запроса)
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Регистрируем Kafka Outbox Publisher как фоновую службу
        var kafkaOptions = new KafkaOutboxOptions();
        configuration.GetSection("KafkaOutbox").Bind(kafkaOptions);
        services.AddSingleton(kafkaOptions);
        services.AddHostedService<KafkaOutboxPublisher>();

        return services;
    }
}
