using Eduflex.DTOs.Address;
using Eduflex.DTOs.DynamicForm;
using Eduflex.DTOs.Enrolment;
using Eduflex.DTOs.VisaProcess;

namespace Eduflex.DTOs.MigrationCase
{
    public class MigrationCaseStepDto
    {
        public string Key { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Phase { get; set; }
        public bool CanReopen { get; set; }
        public string? PractitionerTagId { get; set; }
        public List<StepFieldDefinitionDto> FieldsSnapshot { get; set; } = new();
        public List<string> RequiredEvidenceCategoriesSnapshot { get; set; } = new();
        public List<StepPreconditionDto> PreconditionsSnapshot { get; set; } = new();
        public List<ProcessStepHintDto> HintsSnapshot { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new();
        public DateTime? CompletedAt { get; set; }
        public string? CompletedByName { get; set; }
    }

    public class MigrationCaseDocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Note { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class MigrationCaseAuditEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }

    // Mirrors EnrolmentCommunicationDto/SendEnrolmentCommunicationDto exactly — same
    // underlying EnrolmentCommunicationModel shape (see MigrationCaseModel.Communications).
    public class MigrationCaseCommunicationDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TemplateKey { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> AttachedDocumentIds { get; set; } = new();
        public string SentByName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class SendMigrationCaseCommunicationDto
    {
        public string? TemplateKey { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> AttachedDocumentIds { get; set; } = new();
    }

    // Mirrors EnrolmentFormResponseDto/RequestFormDto/SetFormResponseStatusDto exactly —
    // same underlying EnrolmentFormResponseModel shape (see MigrationCaseModel.FormResponses).
    public class MigrationCaseFormResponseDto
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

    public class RequestMigrationCaseFormDto
    {
        public string FormTemplateId { get; set; } = string.Empty;
    }

    // Shared body for both draft-save and submit — MarkSubmitted selects which (see
    // MigrationCaseService.SaveFormAnswersAsync's header comment for why there's one
    // staff-driven write path instead of Enrolment's separate student draft/submit).
    public class SaveMigrationCaseFormAnswersDto
    {
        public List<FormAnswerDto> Answers { get; set; } = new();
        public bool MarkSubmitted { get; set; }
    }

    public class SetMigrationCaseFormStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class MigrationCaseDto
    {
        public string Id { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public string CaseReference { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public int TemplateVersion { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PrimaryContactName { get; set; } = string.Empty;
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactMobile { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
        public AddressDto? HometownAddress { get; set; }
        public AddressDto? CurrentAddress { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<MigrationCaseStepDto> Steps { get; set; } = new();
        public List<MigrationCaseDocumentDto> Documents { get; set; } = new();
        public List<MigrationCaseCommunicationDto> Communications { get; set; } = new();
        public List<MigrationCaseFormResponseDto> FormResponses { get; set; } = new();
        public List<MigrationCaseAuditEntryDto> AuditTrail { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMigrationCaseDto
    {
        public string TemplateId { get; set; } = string.Empty;
        public string PrimaryContactName { get; set; } = string.Empty;
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactMobile { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCustomerInfoDto
    {
        public string PrimaryContactName { get; set; } = string.Empty;
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactMobile { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
        public AddressDto? HometownAddress { get; set; }
        public AddressDto? CurrentAddress { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
    }

    public class SaveMigrationCaseStepDto
    {
        public Dictionary<string, string> Fields { get; set; } = new();
    }

    public class AddMigrationCaseDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Note { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
    }

    public class ReassignMigrationCaseDto
    {
        public string NewOwnerUserId { get; set; } = string.Empty;
    }

    // Query shape for the paged list endpoint — mirrors EnrolmentFilterDto's
    // POST-with-body-filter convention (ShareService.Common.PagedResult<MigrationCaseDto>
    // is the actual response type, not a bespoke wrapper).
    public class MigrationCaseFilterDto
    {
        public List<string>? Statuses { get; set; }
        public string? OwnerUserId { get; set; }
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
