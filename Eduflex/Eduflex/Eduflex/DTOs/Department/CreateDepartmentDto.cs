namespace Eduflex.DTOs.Department
{
    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentDepartmentId { get; set; }
        public string? HeadUserId { get; set; }
        public List<string> MemberUserIds { get; set; } = new();
    }
}
