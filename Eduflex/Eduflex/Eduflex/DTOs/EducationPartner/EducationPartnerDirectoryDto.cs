namespace Eduflex.DTOs.EducationPartner
{
    // Public-safe subset of EducationPartnerDto for the unauthenticated-by-permission
    // student application directory (EducationPartnersController.GetEducationPartnersDirectory).
    // Deliberately excludes BusinessPartnerId/CommissionBaseRate/Abn/Acn — internal
    // commercial terms students should never see.
    public class EducationPartnerDirectoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Trademark { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PartnerType { get; set; } = string.Empty;
        public List<string> Intakes { get; set; } = new();
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public List<CourseDto> Courses { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
