namespace Eduflex.DTOs.Enquiry
{
    public class CreateEnquiryDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Enquiry { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? CoursePromotionId { get; set; }
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}