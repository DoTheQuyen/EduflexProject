using FluentValidation;
using ShareService.Models.Role;

namespace ShareService.Validations.Role
{
    public class CreateRoleModelValidator : AbstractValidator<CreateRoleModel>
    {
        public CreateRoleModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required")
                .MaximumLength(50).WithMessage("Role name must not exceed 50 characters");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Description must not exceed 200 characters");

            RuleFor(x => x.PermissionIds)
                .NotNull().WithMessage("PermissionIds must be provided (can be an empty list)");
        }
    }
}
