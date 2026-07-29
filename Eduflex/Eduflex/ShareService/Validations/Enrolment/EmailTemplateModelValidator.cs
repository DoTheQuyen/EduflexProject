using FluentValidation;
using ShareService.Models.Enrolment;

namespace ShareService.Validations.Enrolment
{
    public class EmailTemplateModelValidator : AbstractValidator<EmailTemplateModel>
    {
        public EmailTemplateModelValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Key is required")
                .Matches("^[a-z0-9-]+$").WithMessage("Key must be lowercase letters, numbers and hyphens only")
                .MaximumLength(60).WithMessage("Key must not exceed 60 characters");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required")
                .MaximumLength(200).WithMessage("Subject must not exceed 200 characters");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Body is required")
                .MaximumLength(5000).WithMessage("Body must not exceed 5000 characters");
        }
    }
}
