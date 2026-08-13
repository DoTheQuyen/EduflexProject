using FluentValidation;
using ShareService.Enums.DynamicForms;
using ShareService.Models.DynamicForm;

namespace ShareService.Validations.DynamicForm
{
    public class DynamicFormTemplateModelValidator : AbstractValidator<DynamicFormTemplateModel>
    {
        public DynamicFormTemplateModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Form name is required")
                .MaximumLength(150).WithMessage("Form name must not exceed 150 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(status => Enum.TryParse<TemplateStatus>(status, out _))
                .WithMessage("Status is not a recognised form status");

            // Not validated against a fixed step-key list — a form template is shared
            // across modules (see EnrolmentFormResponseModel's reuse on
            // MigrationCaseModel.FormResponses), and each module's valid step keys come
            // from a different source: Enrolment's fixed VisaProcessStepKeys.Ordered, a
            // Migration Case's whichever VisaProcessTemplate it's running. Enforcing
            // membership in one module's list would make the field unusable for the other.
            RuleFor(x => x.BoundStepKey)
                .MaximumLength(100).WithMessage("Bound step key must not exceed 100 characters");

            RuleFor(x => x.Questions)
                .NotEmpty().WithMessage("A form needs at least one question");

            RuleForEach(x => x.Questions).ChildRules(question =>
            {
                question.RuleFor(q => q.QuestionText)
                    .NotEmpty().WithMessage("Question text is required")
                    .MaximumLength(500).WithMessage("Question text must not exceed 500 characters");

                question.RuleFor(q => q.AnswerType)
                    .NotEmpty().WithMessage("Answer type is required")
                    .Must(type => Enum.TryParse<AnswerType>(type, out _))
                    .WithMessage("Answer type is not recognised");

                question.RuleFor(q => q.Options)
                    .Must(options => options.Count >= 2)
                    .When(q => q.AnswerType == AnswerType.SingleSelect.ToString() || q.AnswerType == AnswerType.MultiSelect.ToString())
                    .WithMessage("Single-select and multi-select questions need at least two options");

                question.RuleFor(q => q.MaxLength)
                    .GreaterThan(0).When(q => q.MaxLength.HasValue)
                    .WithMessage("Max length must be greater than zero");
            });
        }
    }
}
