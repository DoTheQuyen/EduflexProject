using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.DynamicForms;
using ShareService.Models.Common;

namespace ShareService.Models.DynamicForm
{
    public class DynamicFormTemplateModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = TemplateStatus.Active.ToString();

        // Free-form, not validated against a fixed list (see
        // DynamicFormTemplateModelValidator) — a template is shared across modules, and
        // each has its own valid step keys: Enrolment's fixed VisaProcessStepKeys.Ordered,
        // or a Migration Case's own VisaProcessTemplate steps. Null means the template
        // isn't bound to a step — still requestable ad hoc, it just has no step marker
        // on the Process tab and its exported PDF lands in the generic "DynamicForm"
        // document category instead of replacing a specific step's evidence file.
        [BsonElement("boundStepKey")]
        public string? BoundStepKey { get; set; }

        [BsonElement("questions")]
        public List<FormQuestionModel> Questions { get; set; } = new();
    }

    public class FormQuestionModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("order")]
        public int Order { get; set; }

        [BsonElement("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [BsonElement("helpText")]
        public string? HelpText { get; set; }

        [BsonElement("answerType")]
        public string AnswerType { get; set; } = string.Empty;

        // Only populated for SingleSelect / MultiSelect questions.
        [BsonElement("options")]
        public List<string> Options { get; set; } = new();

        [BsonElement("isRequired")]
        public bool IsRequired { get; set; }

        // Only meaningful for RichText questions. Null = use the system default
        // (DynamicFormLimits.RichTextAnswerMaxLength) — most questions don't need a
        // custom limit, only the ones that genuinely need more/less room.
        [BsonElement("maxLength")]
        public int? MaxLength { get; set; }

        // Only meaningful for SingleSelect / MultiSelect questions. False = vertical
        // (default), true = horizontal.
        [BsonElement("optionsHorizontal")]
        public bool OptionsHorizontal { get; set; }
    }
}
