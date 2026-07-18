using FluentValidation;
using ShareService.Models.Application;

namespace ShareService.Validations.Application
{
    public class CreateApplicationModelValidator : AbstractValidator<CreateApplicationModel>
    {
        public CreateApplicationModelValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required")
                .MaximumLength(50).WithMessage("Student ID must not exceed 50 characters");

            RuleFor(x => x.StudentName)
                .NotEmpty().WithMessage("Student name is required")
                .MaximumLength(100).WithMessage("Student name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.ApplicationType)
                .NotEmpty().WithMessage("Application type is required")
                .MaximumLength(50).WithMessage("Application type must not exceed 50 characters");

            RuleFor(x => x.Details)
                .MaximumLength(2000).WithMessage("Details must not exceed 2000 characters");
        }
    }

}