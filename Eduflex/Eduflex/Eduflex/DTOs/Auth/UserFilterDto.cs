namespace Eduflex.DTOs.Auth
{
    public class UserFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? RoleId { get; set; }
        public List<string>? RoleIds { get; set; }
        public bool? IsActive { get; set; }
    }
}