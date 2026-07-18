using FluentValidation;
using ShareService.Models.Auth;

namespace ShareService.Validations.Auth
{
    public class LoginModelValidator : AbstractValidator<LoginModel>
    {
        public LoginModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email address is required")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .MaximumLength(50).WithMessage("Password must not exceed 50 characters");
                //.Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$")
                //.WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number");
        }
    }
}