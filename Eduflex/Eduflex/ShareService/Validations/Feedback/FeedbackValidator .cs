using FluentValidation;
using ShareService.Models.Feedback;

namespace ShareService.Validations.Feedback
{
    public class FeedbackModelValidator : AbstractValidator<FeedbackModel>
    {
        public FeedbackModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

            RuleFor(x => x.PhotoData)
                .NotEmpty().WithMessage("Photo is required")
                .MaximumLength(200000).WithMessage("Photo is too large - please use a smaller image");

            RuleFor(x => x.PhotoContentType)
                .NotEmpty().WithMessage("Photo content type is required")
                .Must(t => t == "image/jpeg" || t == "image/png")
                .WithMessage("Photo must be a JPEG or PNG image");

            RuleFor(x => x.CourseName)
                .NotEmpty().WithMessage("Course name is required")
                .MaximumLength(150).WithMessage("Course name must not exceed 150 characters");

            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Comment is required")
                .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");
        }
    }
}