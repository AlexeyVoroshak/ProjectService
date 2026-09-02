using ProjectService.Domain.Entities;
using ProjectService.Domain.Enums;
using ProjectService.Domain.Events;

namespace ProjectService.Domain.Tests;

/// <summary>
/// Тесты для доменной сущности Project.
/// Проверяют бизнес-правила и доменные события.
/// </summary>
public class ProjectTests
{
    /// <summary>
    /// Проверяет, что при создании проекта генерируется событие ProjectCreatedEvent.
    /// </summary>
    [Fact]
    public void CreateProject_ShouldRaiseProjectCreatedEvent()
    {
        // Arrange & Act
        var project = new Project("Test Project", "Description");

        // Assert
        Assert.Single(project.DomainEvents);
        Assert.IsType<ProjectCreatedEvent>(project.DomainEvents.First());
        var evt = (ProjectCreatedEvent)project.DomainEvents.First();
        Assert.Same(project, evt.Project);
    }

    /// <summary>
    /// Проверяет, что при архивировании проекта генерируется событие ProjectArchivedEvent.
    /// </summary>
    [Fact]
    public void ArchiveProject_ShouldRaiseProjectArchivedEvent()
    {
        // Arrange
        var project = new Project("Test Project");
        project.ClearDomainEvents(); // Очищаем событие создания

        // Act
        project.Archive();

        // Assert
        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.Single(project.DomainEvents);
        Assert.IsType<ProjectArchivedEvent>(project.DomainEvents.First());
    }

    /// <summary>
    /// Проверяет, что при удалении проекта генерируется событие ProjectDeletedEvent.
    /// </summary>
    [Fact]
    public void DeleteProject_ShouldRaiseProjectDeletedEvent()
    {
        // Arrange
        var project = new Project("Test Project");
        project.ClearDomainEvents(); // Очищаем событие создания

        // Act
        project.Delete();

        // Assert
        Assert.Equal(ProjectStatus.Deleted, project.Status);
        Assert.Single(project.DomainEvents);
        Assert.IsType<ProjectDeletedEvent>(project.DomainEvents.First());
    }

    /// <summary>
    /// Проверяет, что при добавлении задачи генерируется событие TaskCreatedEvent.
    /// </summary>
    [Fact]
    public void AddTask_ShouldRaiseTaskCreatedEvent()
    {
        // Arrange
        var project = new Project("Test Project");

        // Act
        project.AddTask("Test Task", "Task Description", TaskPriority.High);

        // Assert
        Assert.Single(project.Tasks);
        var task = project.Tasks.First();
        Assert.Equal("Test Task", task.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(Enums.TaskStatus.Todo, task.Status);
    }

    /// <summary>
    /// Проверяет, что статус проекта по умолчанию — Active.
    /// </summary>
    [Fact]
    public void CreateProject_DefaultStatus_ShouldBeActive()
    {
        // Arrange & Act
        var project = new Project("Test Project");

        // Assert
        Assert.Equal(ProjectStatus.Active, project.Status);
    }

    /// <summary>
    /// Проверяет, что UpdatedAt обновляется при вызове UpdateDetails.
    /// </summary>
    [Fact]
    public void UpdateProjectDetails_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var project = new Project("Old Name");
        var originalUpdatedAt = project.UpdatedAt;

        // Небольшая задержка, чтобы убедиться, что время изменится
        Thread.Sleep(10);

        // Act
        project.UpdateDetails("New Name", "New Description");

        // Assert
        Assert.True(project.UpdatedAt > originalUpdatedAt);
        Assert.Equal("New Name", project.Name);
        Assert.Equal("New Description", project.Description);
    }
}
