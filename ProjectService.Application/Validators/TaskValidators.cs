using FluentValidation;
using ProjectService.Application.Features.Tasks.Commands;

namespace ProjectService.Application.Validators;

/// <summary>
/// Валидатор для команды создания задачи.
/// </summary>
public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Идентификатор проекта обязателен");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название задачи обязательно")
            .MaximumLength(200).WithMessage("Название задачи не должно превышать 200 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание задачи не должно превышать 2000 символов");
    }
}

/// <summary>
/// Валидатор для команды обновления задачи.
/// </summary>
public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Идентификатор задачи обязателен");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название задачи обязательно")
            .MaximumLength(200).WithMessage("Название задачи не должно превышать 200 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание задачи не должно превышать 2000 символов");
    }
}
