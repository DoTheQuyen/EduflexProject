using ShareService.Enums.Roles;

namespace ShareService.Common
{
    public class EnquiryFilter : PaginationQuery
    {
        public EnquiryEnums? Status { get; set; }
    }
}
