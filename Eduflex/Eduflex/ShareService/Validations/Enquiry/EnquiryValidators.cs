using FluentValidation;
using ShareService.Enums.Roles;
using ShareService.Models.Enquiry;

namespace ShareService.Validations.Enquiry
{
    public class EnquiryModelValidator : AbstractValidator<EnquiryModel>
    {
        public EnquiryModelValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Middle name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email is not a valid email address")
                .MaximumLength(150).WithMessage("Email must not exceed 150 characters");

            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("Mobile number is required")
                .Matches(@"^\+?[0-9\s\-()]{7,20}$").WithMessage("Mobile number is not valid");

            RuleFor(x => x.Enquiry)
                .NotEmpty().WithMessage("Enquiry is required")
                .MaximumLength(2000).WithMessage("Enquiry must not exceed 2000 characters");

            RuleSet("Create", () =>
            {
                RuleFor(x => x.RecaptchaToken)
                    .NotEmpty().WithMessage("Please verify that you are not a robot");
            });

            RuleSet("Update", () =>
            {
                RuleFor(x => x.Status)
                    .NotEqual(EnquiryEnums.New.ToString())
                    .WithMessage("Status must be changed when responding to customer");

                RuleFor(x => x.Response)
                    .NotEmpty().WithMessage("A response message is required when responding to customer")
                    .When(x => x.Status != EnquiryEnums.New.ToString());

                RuleFor(x => x.Response)
                    .MaximumLength(5000).WithMessage("Response must not exceed 5000 characters");
            });
        }
    }
}
