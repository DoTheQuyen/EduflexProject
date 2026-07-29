using FluentValidation;
using ShareService.Models.Student;

namespace ShareService.Validations.Student
{
    public class StudentModelValidator : AbstractValidator<StudentModel>
    {
        public StudentModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email address is required")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

            RuleFor(x => x.Nationality)
                .NotEmpty().WithMessage("Nationality is required")
                .MaximumLength(50).WithMessage("Nationality must not exceed 50 characters");

            RuleFor(x => x.PassportNumber)
                .NotEmpty().WithMessage("Passport number is required")
                .MaximumLength(20).WithMessage("Passport number must not exceed 20 characters");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters");

            RuleFor(x => x.Address)
                .NotNull().WithMessage("Address is required");
        }
    }
}
