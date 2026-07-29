using FluentValidation;
using ShareService.Enums.Roles;
using ShareService.Models.Enrolment;

namespace ShareService.Validations.Enrolment
{
    public class EnrolmentModelValidator : AbstractValidator<EnrolmentModel>
    {
        public EnrolmentModelValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email is not a valid email address")
                .MaximumLength(150).WithMessage("Email must not exceed 150 characters");

            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("Mobile is required")
                .Matches(@"^\+?[0-9\s\-()]{7,20}$").WithMessage("Mobile number is not valid");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(status => Enum.TryParse<EnrolmentEnums>(status, out _))
                .WithMessage("Status is not a recognised enrolment status");

            RuleFor(x => x.TuitionFee)
                .GreaterThanOrEqualTo(0).When(x => x.TuitionFee.HasValue)
                .WithMessage("Tuition fee must not be negative");

            RuleSet("Create", () =>
            {
                RuleFor(x => x.EducationPartnerId)
                    .NotEmpty().WithMessage("University is required");

                RuleFor(x => x.CourseId)
                    .NotEmpty().WithMessage("Course is required");
            });
        }
    }
}
