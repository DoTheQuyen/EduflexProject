namespace Eduflex.DTOs.EducationPartner
{
    public class EducationPartnerDto
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
        public string? BusinessPartnerId { get; set; }
        public string? BusinessPartnerName { get; set; }
        public decimal CommissionBaseRate { get; set; }
        public string? Abn { get; set; }
        public string? Acn { get; set; }
        public List<CourseDto> Courses { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
