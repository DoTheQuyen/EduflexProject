using Eduflex.DTOs.Address;
using Eduflex.DTOs.DynamicForm;
using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Enrolment
{
    public class EnrolmentDto
    {
        public string Id { get; set; } = string.Empty;

        public string OwnerUserId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string StudentUserId { get; set; } = string.Empty;
        public string? StudentApplicationId { get; set; }
        public string? EnquiryId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
        public AddressDto? HometownAddress { get; set; }
        public AddressDto? CurrentAddress { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }

        public string? EducationPartnerId { get; set; }
        public string? CourseId { get; set; }
        public string? Intake { get; set; }
        public string? StudyMode { get; set; }
        public string? Campus { get; set; }
        public DateTime? CommencementDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public string? FundingSource { get; set; }
        public string? VisaStatus { get; set; }
        public string Status { get; set; } = EnrolmentEnums.Draft.ToString();
        public string? Notes { get; set; }

        public List<EnrolmentDocumentDto> Documents { get; set; } = new();
        public List<EnrolmentCommunicationDto> Communications { get; set; } = new();
        public List<EnrolmentFormResponseDto> FormResponses { get; set; } = new();
        public List<EnrolmentAuditEntryDto> AuditTrail { get; set; } = new();
        public List<VisaProcessStepDto> VisaProcessSteps { get; set; } = new();
        public List<CourseApplicationDto> CourseApplications { get; set; } = new();
        public int? MaxApplicationsOverride { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
