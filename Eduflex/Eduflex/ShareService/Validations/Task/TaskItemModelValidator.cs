using FluentValidation;
using ShareService.Models.Task;

namespace ShareService.Validations.Task
{
    public class TaskItemModelValidator : AbstractValidator<TaskItemModel>
    {
        public TaskItemModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Task name is required")
                .MaximumLength(200).WithMessage("Task name must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

            RuleFor(x => x.AssigneeUserId)
                .NotEmpty().WithMessage("An assignee is required");

            RuleFor(x => x.DueDateTime)
                .NotEqual(default(DateTime)).WithMessage("A due date/time is required");
        }
    }
}
