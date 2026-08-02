namespace Eduflex.DTOs.Department
{
    public class DepartmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentDepartmentId { get; set; }
        public string? HeadUserId { get; set; }
        public List<string> MemberUserIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
