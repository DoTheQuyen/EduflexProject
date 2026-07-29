using ShareService.Common;

namespace ShareService.Models.Auth
{
    public class UserFilter : PaginationQuery
    {
        public string? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}
