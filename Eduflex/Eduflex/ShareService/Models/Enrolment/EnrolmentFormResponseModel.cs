using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.DynamicForms;
using ShareService.Models.DynamicForm;

namespace ShareService.Models.Enrolment
{
    // Embedded in EnrolmentModel.FormResponses, sibling to Documents/Communications/
    // AuditTrail/VisaProcessSteps — a form response only ever belongs to one enrolment
    // and is always read/written in that context, same reasoning that already kept
    // Documents/Communications embedded rather than a separate FK'd collection.
    public class EnrolmentFormResponseModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("formTemplateId")]
        public string FormTemplateId { get; set; } = string.Empty;

        // Snapshotted at request time so a later template edit never retroactively
        // changes a form the student already started or finished.
        [BsonElement("formName")]
        public string FormName { get; set; } = string.Empty;

        [BsonElement("questionsSnapshot")]
        public List<FormQuestionModel> QuestionsSnapshot { get; set; } = new();

        [BsonElement("boundStepKey")]
        public string? BoundStepKey { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = ResponseStatus.Requesting.ToString();

        [BsonElement("answers")]
        public List<FormAnswerModel> Answers { get; set; } = new();

        [BsonElement("requestedByUserId")]
        public string RequestedByUserId { get; set; } = string.Empty;

        [BsonElement("requestedByName")]
        public string RequestedByName { get; set; } = string.Empty;

        [BsonElement("requestedAt")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("lastSavedAt")]
        public DateTime? LastSavedAt { get; set; }

        [BsonElement("submittedAt")]
        public DateTime? SubmittedAt { get; set; }

        [BsonElement("withdrawnAt")]
        public DateTime? WithdrawnAt { get; set; }

        [BsonElement("withdrawnByUserId")]
        public string? WithdrawnByUserId { get; set; }

        // Set when staff edit the student's finalized answers directly — the audit
        // trail already records the "why", these two just surface "when/who" inline
        // on the Forms tab without needing to open Audit Trail.
        [BsonElement("staffEditedAt")]
        public DateTime? StaffEditedAt { get; set; }

        [BsonElement("staffEditedByUserId")]
        public string? StaffEditedByUserId { get; set; }

        // Always points at the *latest* auto-generated PDF (EnrolmentDocumentModel.Id) —
        // set by SubmitFormAsync and refreshed by StaffEditFormResponseAsync. See
        // docs/08-dynamic-form-module-design.md §2.4 for why this is automatic, not a
        // manual "save as document" action.
        [BsonElement("exportedDocumentId")]
        public string? ExportedDocumentId { get; set; }
    }

    public class FormAnswerModel
    {
        [BsonElement("questionId")]
        public string QuestionId { get; set; } = string.Empty;

        // RichText answer text, or "Yes"/"No" for YesNo questions.
        [BsonElement("textValue")]
        public string? TextValue { get; set; }

        // SingleSelect (list of 1) / MultiSelect.
        [BsonElement("selectedOptions")]
        public List<string> SelectedOptions { get; set; } = new();
    }
}
