namespace ShareService.Models.Auth
{
    public class UserKeysetFilter
    {
        public string? SearchTerm { get; set; }
        public string? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public int PageSize { get; set; } = 20;

        public DateTime? AfterCreatedAt { get; set; }
        public string? AfterId { get; set; }
    }
}