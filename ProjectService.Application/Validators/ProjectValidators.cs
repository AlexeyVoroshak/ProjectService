using FluentValidation;
using ProjectService.Application.Features.Projects.Commands;

namespace ProjectService.Application.Validators;

/// <summary>
/// Валидатор для команды создания проекта.
/// Проверяет обязательные поля и ограничения на длину.
/// </summary>
public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название проекта обязательно")
            .MaximumLength(100).WithMessage("Название проекта не должно превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание проекта не должно превышать 2000 символов");
    }
}

/// <summary>
/// Валидатор для команды обновления проекта.
/// </summary>
public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Идентификатор проекта обязателен");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название проекта обязательно")
            .MaximumLength(100).WithMessage("Название проекта не должно превышать 100 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание проекта не должно превышать 2000 символов");
    }
}
