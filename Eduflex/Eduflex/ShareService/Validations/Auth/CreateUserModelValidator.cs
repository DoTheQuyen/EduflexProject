using FluentValidation;
using ShareService.Models.Auth;

namespace ShareService.Validations.Auth
{
    public class CreateUserModelValidator : AbstractValidator<UserModel>
    {
        public CreateUserModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email address is required")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

            RuleSet("Create", () =>
            {
                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required")
                    .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                    .MaximumLength(50).WithMessage("Password must not exceed 50 characters");

                RuleFor(x => x.IsActive)
               .NotEmpty().WithMessage("User activation status is required");
            });

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

            RuleFor(x => x.MiddleName)
                .MaximumLength(50).WithMessage("Middle name must not exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("Mobile is required")
                .MaximumLength(20).WithMessage("Mobile must not exceed 20 characters");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("RoleId is required");
            
        }
    }
}
