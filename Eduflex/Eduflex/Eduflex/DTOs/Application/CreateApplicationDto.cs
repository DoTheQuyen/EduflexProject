using Eduflex.DTOs.Address;
using Eduflex.DTOs.Enrolment;

namespace Eduflex.DTOs.Application
{
    public class CreateApplicationDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
        public string? StudyMode { get; set; }
        public string? Campus { get; set; }
        public AddressDto? HometownAddress { get; set; }
        public AddressDto? CurrentAddress { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
    }
}