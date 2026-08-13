using FluentValidation;
using ShareService.Enums.VisaProcess;
using ShareService.Models.VisaProcess;

namespace ShareService.Validations.VisaProcess
{
    public class VisaProcessTemplateModelValidator : AbstractValidator<VisaProcessTemplateModel>
    {
        public VisaProcessTemplateModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required")
                .MaximumLength(150).WithMessage("Template name must not exceed 150 characters");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(50).WithMessage("Country must not exceed 50 characters");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .MaximumLength(80).WithMessage("Category must not exceed 80 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(status => Enum.TryParse<VisaTemplateStatus>(status, out _))
                .WithMessage("Status is not a recognised template status");

            RuleFor(x => x.Steps)
                .NotEmpty().WithMessage("A template needs at least one step");

            RuleForEach(x => x.Steps).ChildRules(step =>
            {
                step.RuleFor(s => s.Key)
                    .NotEmpty().WithMessage("Step key is required")
                    .MaximumLength(80).WithMessage("Step key must not exceed 80 characters");

                step.RuleFor(s => s.Label)
                    .NotEmpty().WithMessage("Step label is required")
                    .MaximumLength(150).WithMessage("Step label must not exceed 150 characters");

                step.RuleForEach(s => s.Fields).ChildRules(field =>
                {
                    field.RuleFor(f => f.FieldKey)
                        .NotEmpty().WithMessage("Field key is required")
                        .MaximumLength(80).WithMessage("Field key must not exceed 80 characters");

                    field.RuleFor(f => f.Label)
                        .NotEmpty().WithMessage("Field label is required")
                        .MaximumLength(150).WithMessage("Field label must not exceed 150 characters");

                    field.RuleFor(f => f.InputType)
                        .NotEmpty().WithMessage("Field input type is required")
                        .Must(type => Enum.TryParse<StepFieldInputType>(type, out _))
                        .WithMessage("Field input type is not recognised");

                    field.RuleFor(f => f.Options)
                        .Must(options => options.Count >= 2)
                        .When(f => f.InputType == StepFieldInputType.Select.ToString())
                        .WithMessage("A Select field needs at least two options");
                });

                step.RuleForEach(s => s.Preconditions).ChildRules(precondition =>
                {
                    precondition.RuleFor(p => p.Type)
                        .NotEmpty().WithMessage("Precondition type is required")
                        .Must(type => Enum.TryParse<StepPreconditionType>(type, out _))
                        .WithMessage("Precondition type is not recognised");
                });

                step.RuleForEach(s => s.Hints).ChildRules(hint =>
                {
                    hint.RuleFor(h => h.Text)
                        .NotEmpty().WithMessage("Hint text is required")
                        .MaximumLength(1000).WithMessage("Hint text must not exceed 1000 characters");
                });
            });
        }
    }
}
