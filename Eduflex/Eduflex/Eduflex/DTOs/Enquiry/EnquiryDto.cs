using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Enquiry
{
    public class EnquiryDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Enquiry { get; set; } = string.Empty;
        public string Status { get; set; } = EnquiryEnums.New.ToString();
        public string? Response { get; set; }
    }
}
