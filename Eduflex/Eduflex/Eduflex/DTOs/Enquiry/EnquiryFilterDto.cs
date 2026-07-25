using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Enquiry
{
    public class EnquiryFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public EnquiryEnums? Status { get; set; }
    }
}
