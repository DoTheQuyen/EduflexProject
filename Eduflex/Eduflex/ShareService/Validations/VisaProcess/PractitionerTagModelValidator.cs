using FluentValidation;
using ShareService.Models.VisaProcess;

namespace ShareService.Validations.VisaProcess
{
    public class PractitionerTagModelValidator : AbstractValidator<PractitionerTagModel>
    {
        public PractitionerTagModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tag name is required")
                .MaximumLength(80).WithMessage("Tag name must not exceed 80 characters");

            RuleFor(x => x.ColorHex)
                .NotEmpty().WithMessage("Colour is required")
                .Matches("^#[0-9a-fA-F]{6}$").WithMessage("Colour must be a 6-digit hex value, e.g. #b8862f");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Description must not exceed 300 characters");
        }
    }
}
