using FluentValidation;
using ShareService.Models.CoursePromotion;

namespace ShareService.Validations.CoursePromotion
{
    public class CoursePromotionModelValidator : AbstractValidator<CoursePromotionModel>
    {
        public CoursePromotionModelValidator()
        {
            RuleFor(x => x.CourseName)
                .NotEmpty().WithMessage("Course name is required")
                .MaximumLength(150).WithMessage("Course name must not exceed 150 characters");

            RuleFor(x => x.UniversityName)
                .NotEmpty().WithMessage("University name is required")
                .MaximumLength(150).WithMessage("University name must not exceed 150 characters");

            RuleFor(x => x.Semester)
                .NotEmpty().WithMessage("Semester is required")
                .MaximumLength(50).WithMessage("Semester must not exceed 50 characters");

            RuleFor(x => x.ScholarshipLabel)
                .NotEmpty().WithMessage("Scholarship label is required")
                .MaximumLength(80).WithMessage("Scholarship label must not exceed 80 characters");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required")
                .MaximumLength(100).WithMessage("Location must not exceed 100 characters");

            RuleFor(x => x.Tuition)
                .NotEmpty().WithMessage("Tuition is required")
                .MaximumLength(80).WithMessage("Tuition must not exceed 80 characters");

            RuleFor(x => x.Opportunities)
                .NotEmpty().WithMessage("Opportunities is required")
                .MaximumLength(150).WithMessage("Opportunities must not exceed 150 characters");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Offer end date must be in the future");

            RuleFor(x => x.Note)
                .MaximumLength(600).WithMessage("Note must not exceed 600 characters");

            RuleFor(x => x.WebsiteUrl)
                .NotEmpty().WithMessage("University website URL is required")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("University website URL must be a valid http(s) URL");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative");
        }
    }
}
