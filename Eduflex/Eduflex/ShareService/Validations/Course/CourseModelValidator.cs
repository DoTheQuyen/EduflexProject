using FluentValidation;
using ShareService.Models.Course;

namespace ShareService.Validations.Course
{
    public class CourseModelValidator : AbstractValidator<CourseModel>
    {
        public CourseModelValidator()
        {
            RuleFor(x => x.EducationPartnerId)
                .NotEmpty().WithMessage("Education partner is required");

            RuleFor(x => x.CourseName)
                .NotEmpty().WithMessage("Course name is required")
                .MaximumLength(150).WithMessage("Course name must not exceed 150 characters");

            RuleFor(x => x.Intakes)
                .NotEmpty().WithMessage("At least one intake is required");

            RuleFor(x => x.TuitionFee)
                .GreaterThan(0).WithMessage("Tuition fee must be greater than 0");

            RuleFor(x => x.TotalTuitionFee)
                .GreaterThan(0).WithMessage("Total course tuition must be greater than 0");

            RuleFor(x => x.TuitionCurrency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be a 3-letter code");

            RuleFor(x => x.CommissionBaseRate)
                .InclusiveBetween(0, 100).WithMessage("Commission base rate must be between 0 and 100");

            RuleFor(x => x.CourseDurationMonths)
                .GreaterThan(0).When(x => x.CourseDurationMonths.HasValue)
                .WithMessage("Course duration must be greater than 0 months");
        }
    }
}
