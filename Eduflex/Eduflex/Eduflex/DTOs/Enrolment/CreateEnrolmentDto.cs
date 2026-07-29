using Eduflex.DTOs.Address;

namespace Eduflex.DTOs.Enrolment
{
    public class CreateEnrolmentDto
    {
        // Set when Step 1 of the New Enrolment wizard resolved to an existing Students
        // collection record (found via search or just created inline) — tells the service
        // to link to that student's existing login instead of creating a second one.
        public string? StudentId { get; set; }

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
        public string? Notes { get; set; }
    }
}
