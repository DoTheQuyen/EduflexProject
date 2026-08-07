using FluentValidation;
using ShareService.Models.Invoice;

namespace ShareService.Validations.Invoice
{
    public class InvoiceTemplateModelValidator : AbstractValidator<InvoiceTemplateModel>
    {
        public InvoiceTemplateModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Category)
                .Must(c => c == InvoiceTemplateCategories.Customer || c == InvoiceTemplateCategories.Partner || c == InvoiceTemplateCategories.Custom)
                .WithMessage("Category must be Customer, Partner or Custom");

            RuleFor(x => x.SenderName)
                .NotEmpty().WithMessage("Sender name is required")
                .MaximumLength(150).WithMessage("Sender name must not exceed 150 characters");

            RuleFor(x => x.SenderEmail)
                .NotEmpty().WithMessage("Sender email is required")
                .EmailAddress().WithMessage("Sender email must be a valid email address");

            RuleFor(x => x.InvoiceNoPrefix)
                .NotEmpty().WithMessage("Invoice number prefix is required")
                .MaximumLength(40).WithMessage("Invoice number prefix must not exceed 40 characters");

            RuleFor(x => x.NumberPadding)
                .InclusiveBetween(1, 8).WithMessage("Number padding must be between 1 and 8");

            RuleFor(x => x.DefaultAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Default amount can't be negative")
                .When(x => x.DefaultAmount.HasValue);

            RuleFor(x => x.DefaultGstRatePercent)
                .InclusiveBetween(0, 100).WithMessage("Default GST rate must be between 0 and 100")
                .When(x => x.DefaultGstRatePercent.HasValue);
        }
    }
}
