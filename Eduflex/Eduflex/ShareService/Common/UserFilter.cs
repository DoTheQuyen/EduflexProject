namespace ShareService.Common
{
    public class UserFilter : PaginationQuery
    {
        public string? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}
