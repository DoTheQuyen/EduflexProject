using FluentValidation;
using ShareService.Models.Department;

namespace ShareService.Validations.Department
{
    public class DepartmentModelValidator : AbstractValidator<DepartmentModel>
    {
        public DepartmentModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Department name is required")
                .MaximumLength(150).WithMessage("Department name must not exceed 150 characters");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Description must not exceed 300 characters");

            RuleFor(x => x.MemberUserIds)
                .NotNull().WithMessage("MemberUserIds must be provided (can be an empty list)");

            RuleFor(x => x.ParentDepartmentId)
                .NotEqual(x => x.Id).WithMessage("A department cannot be its own parent.")
                .When(x => !string.IsNullOrEmpty(x.ParentDepartmentId));

            RuleFor(x => x.HeadUserId)
                .Must((model, headUserId) => model.MemberUserIds.Contains(headUserId!))
                .WithMessage("The department head must also be a member of the department.")
                .When(x => !string.IsNullOrEmpty(x.HeadUserId));
        }
    }
}
