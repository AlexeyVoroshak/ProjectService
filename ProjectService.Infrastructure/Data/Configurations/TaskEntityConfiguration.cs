using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectService.Domain.Entities;

namespace ProjectService.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API конфигурация для сущности TaskEntity.
/// Определяет маппинг свойств, ограничения и связи.
/// </summary>
public static class TaskEntityConfiguration
{
    /// <summary>
    /// Настраивает модель TaskEntity для EF Core.
    /// </summary>
    /// <param name="builder">Builder для конфигурации сущности</param>
    public static void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        // Определяем имя таблицы в БД
        builder.ToTable("Tasks");

        // Настраиваем первичный ключ
        builder.HasKey(t => t.Id);

        // Настраиваем свойство ProjectId — внешний ключ
        builder.Property(t => t.ProjectId)
            .IsRequired();

        // Настраиваем свойство Title — обязательное, максимальная длина 200
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Настраиваем свойство Description — необязательное
        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // Настраиваем свойство Status — обязательное, хранится как integer
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        // Настраиваем свойство Priority — обязательное, хранится как integer
        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>();

        // Настраиваем свойство CreatedAt
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Настраиваем свойство UpdatedAt
        builder.Property(t => t.UpdatedAt)
            .IsRequired();
    }
}
