using FluentValidation;
using ShareService.Models.BusinessPartner;

namespace ShareService.Validations.BusinessPartner
{
    public class BusinessPartnerModelValidator : AbstractValidator<BusinessPartnerModel>
    {
        public BusinessPartnerModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required");

            RuleFor(x => x.CommissionBaseRate)
                .InclusiveBetween(0, 100).WithMessage("Commission base rate must be between 0 and 100");

            RuleFor(x => x.Abn)
                .Matches(@"^\d{11}$").When(x => !string.IsNullOrEmpty(x.Abn))
                .WithMessage("ABN must be 11 digits");

            RuleFor(x => x.Acn)
                .Matches(@"^\d{9}$").When(x => !string.IsNullOrEmpty(x.Acn))
                .WithMessage("ACN must be 9 digits");

            RuleFor(x => x.ContractEndDate)
                .GreaterThanOrEqualTo(x => x.ContractStartDate!.Value)
                .When(x => x.ContractStartDate.HasValue && x.ContractEndDate.HasValue)
                .WithMessage("Contract end date must be on or after the contract start date");

            RuleForEach(x => x.Contacts).ChildRules(contact =>
            {
                contact.RuleFor(c => c.FirstName).NotEmpty().WithMessage("Contact first name is required");
                contact.RuleFor(c => c.LastName).NotEmpty().WithMessage("Contact last name is required");
                contact.RuleFor(c => c.Email)
                    .NotEmpty().WithMessage("Contact email is required")
                    .EmailAddress().WithMessage("A valid contact email is required");
            });
        }
    }
}
