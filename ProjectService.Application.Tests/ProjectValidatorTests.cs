using FluentValidation.TestHelper;
using ProjectService.Application.Features.Projects.Commands;
using ProjectService.Application.Validators;

namespace ProjectService.Application.Tests;

/// <summary>
/// Тесты для валидаторов Application layer.
/// Проверяют правила валидации для команд.
/// </summary>
public class ProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    /// <summary>
    /// Проверяет, что валидатор проходит при корректных данных.
    /// </summary>
    [Fact]
    public void Validate_ValidCommand_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new CreateProjectCommand("Test Project", "Description");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    /// <summary>
    /// Проверяет, что валидатор отвергает пустое название.
    /// </summary>
    [Fact]
    public void Validate_EmptyName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateProjectCommand("", "Description");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    /// <summary>
    /// Проверяет, что валидатор отверкает название длиннее 100 символов.
    /// </summary>
    [Fact]
    public void Validate_NameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateProjectCommand(new string('a', 101), "Description");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    /// <summary>
    /// Проверяет, что валидатор отверкает описание длиннее 2000 символов.
    /// </summary>
    [Fact]
    public void Validate_DescriptionTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateProjectCommand("Test", new string('a', 2001));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
