using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectService.Domain.Entities;

namespace ProjectService.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API конфигурация для сущности Project.
/// Определяет маппинг свойств, ограничения и связи.
/// </summary>
public static class ProjectConfiguration
{
    /// <summary>
    /// Настраивает модель Project для EF Core.
    /// </summary>
    /// <param name="builder">Builder для конфигурации сущности</param>
    public static void Configure(EntityTypeBuilder<Project> builder)
    {
        // Определяем имя таблицы в БД
        builder.ToTable("Projects");

        // Настраиваем первичный ключ
        builder.HasKey(p => p.Id);

        // Настраиваем свойство Name — обязательное, максимальная длина 100
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Настраиваем свойство Description — необязательное, максимальная длина 2000
        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        // Настраиваем свойство Status — обязательное, хранится как integer
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        // Настраиваем свойство CreatedAt
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Настраиваем свойство UpdatedAt
        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Настраиваем связь One-to-Many с TaskEntity
        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
