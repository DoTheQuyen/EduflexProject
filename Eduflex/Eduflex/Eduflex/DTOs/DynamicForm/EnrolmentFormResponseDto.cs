namespace Eduflex.DTOs.DynamicForm
{
    public class FormAnswerDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string? TextValue { get; set; }
        public List<string> SelectedOptions { get; set; } = new();
    }

    public class EnrolmentFormResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FormTemplateId { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public List<FormQuestionDto> QuestionsSnapshot { get; set; } = new();
        public string? BoundStepKey { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<FormAnswerDto> Answers { get; set; } = new();

        public string RequestedByName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? LastSavedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public DateTime? StaffEditedAt { get; set; }
        public string? ExportedDocumentId { get; set; }
    }

    public class RequestFormDto
    {
        public string FormTemplateId { get; set; } = string.Empty;
    }

    // Manual staff override — see EnrolmentService.SetFormResponseStatusAsync.
    public class SetFormResponseStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // Wraps the student's own forms list with the Enrolment's id, since the
    // Applications module only knows its own applicationId — the student needs the
    // enrolmentId back to call the draft/submit endpoints (both scoped by enrolment id).
    public class MyEnrolmentFormsDto
    {
        public string EnrolmentId { get; set; } = string.Empty;
        public List<EnrolmentFormResponseDto> Forms { get; set; } = new();
    }

    // Shared body shape for staff-edit, student draft-save and student submit —
    // all three are just "here are the current answers", differing only in which
    // service method/permission path the controller routes them to.
    public class SaveFormAnswersDto
    {
        public List<FormAnswerDto> Answers { get; set; } = new();
    }
}
