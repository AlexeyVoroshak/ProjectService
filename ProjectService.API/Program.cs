using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using ProjectService.Application;
using ProjectService.Infrastructure;
using ProjectService.Infrastructure.Data;
using ProjectService.Infrastructure.Messaging;
using Serilog;

// ============================================================
// UTF-8 кодировка для корректного отображения русского текста
// ============================================================
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Серilog — конфигурация логгера
// ============================================================
// Записывает логи в консоль и файл (logs/log-.txt)
// Уровень логирования: Information (Warning и выше для第三方 библиотек)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// Добавление контроллеров и Swagger
// ============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// Регистрация Application layer
// ============================================================
// Включает MediatR (CQRS), FluentValidation и pipeline behavior
builder.Services.AddApplicationServices();

// ============================================================
// Регистрация Infrastructure layer
// ============================================================
// Включает DbContext, репозитории и Kafka Outbox Publisher
builder.Services.AddInfrastructureServices(builder.Configuration);

// ============================================================
// CORS — разрешаем запросы с фронтенда
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============================================================
// Создание БД и применение миграций при запуске
// ============================================================
// Применяет миграции EF Core (создаёт БД, если не существует)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Применение миграций EF Core...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Миграции успешно применены");
        }
        else
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database");
            }
            logger.LogInformation("Подключение к базе данных успешно");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при инициализации базы данных");
        throw;
    }
}

// ============================================================
// Конвейер обработки HTTP-запросов
// ============================================================

// Swagger UI — доступен по адресу /swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serilog middleware — логирует каждый входящий запрос
app.UseSerilogRequestLogging();

// HTTPS редирект (если включён)
app.UseHttpsRedirection();

// CORS policy
app.UseCors("AllowAll");

// Маршрутизация контроллеров
app.MapControllers();

// ============================================================
// Запуск приложения
// ============================================================
app.Run();
