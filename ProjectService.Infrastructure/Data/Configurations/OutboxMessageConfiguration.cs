using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectService.Domain.Entities;

namespace ProjectService.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API конфигурация для сущности OutboxMessage.
/// Определяет маппинг свойств и индексы для эффективного опроса.
/// </summary>
public static class OutboxMessageConfiguration
{
    /// <summary>
    /// Настраивает модель OutboxMessage для EF Core.
    /// </summary>
    /// <param name="builder">Builder для конфигурации сущности</param>
    public static void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // Определяем имя таблицы в БД
        builder.ToTable("OutboxMessages");

        // Настраиваем первичный ключ
        builder.HasKey(o => o.Id);

        // Настраиваем свойство AggregateId — обязательное
        builder.Property(o => o.AggregateId)
            .IsRequired()
            .HasMaxLength(50);

        // Настраиваем свойство EventType — обязательное
        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(200);

        // Настраиваем свойство Payload — обязательное, может быть большим
        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("text");

        // Настраиваем свойство Status — обязательное, хранится как integer
        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        // Настраиваем свойство Version для оптимистичной блокировки
        builder.Property(o => o.Version)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValue(1);

        // Создаём индекс для эффективного поиска неподтверждённых сообщений
        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Status_CreatedAt");
    }
}
