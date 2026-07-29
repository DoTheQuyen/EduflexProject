using FluentValidation;
using ShareService.Models.EducationPartner;

namespace ShareService.Validations.EducationPartner
{
    public class EducationPartnerModelValidator : AbstractValidator<EducationPartnerModel>
    {
        public EducationPartnerModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required")
                .MaximumLength(150).WithMessage("Location must not exceed 150 characters");

            RuleFor(x => x.Trademark)
                .NotEmpty().WithMessage("Trademark is required")
                .MaximumLength(100).WithMessage("Trademark must not exceed 100 characters");

            RuleFor(x => x.LogoUrl)
                .NotEmpty().WithMessage("Logo is required");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(300).WithMessage("Description must not exceed 300 characters");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

            RuleFor(x => x.PartnerType)
                .NotEmpty().WithMessage("Partner type is required")
                .Must(v => v == "University" || v == "College/Education Organization")
                .WithMessage("Partner type must be either 'University' or 'College/Education Organization'");

            RuleFor(x => x.Intakes)
                .NotEmpty().WithMessage("At least one intake is required");

            RuleFor(x => x.CommissionBaseRate)
                .InclusiveBetween(0, 100).WithMessage("Commission base rate must be between 0 and 100");

            RuleFor(x => x.Abn)
                .Matches(@"^\d{11}$").When(x => !string.IsNullOrEmpty(x.Abn))
                .WithMessage("ABN must be 11 digits");

            RuleFor(x => x.Acn)
                .Matches(@"^\d{9}$").When(x => !string.IsNullOrEmpty(x.Acn))
                .WithMessage("ACN must be 9 digits");
        }
    }
}
