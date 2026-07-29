using ShareService.Common;
using ShareService.Enums.Roles;

namespace ShareService.Models.Enquiry
{
    public class EnquiryFilter : PaginationQuery
    {
        public List<EnquiryEnums>? Statuses { get; set; }
    }
}
