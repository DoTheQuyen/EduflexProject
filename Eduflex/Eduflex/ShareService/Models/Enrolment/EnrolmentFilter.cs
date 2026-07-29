using ShareService.Common;
using ShareService.Enums.Roles;

namespace ShareService.Models.Enrolment
{
    public class EnrolmentFilter : PaginationQuery
    {
        public List<EnrolmentEnums>? Statuses { get; set; }
        public string? OwnerUserId { get; set; }
    }
}
