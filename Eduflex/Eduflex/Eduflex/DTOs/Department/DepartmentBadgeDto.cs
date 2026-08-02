namespace Eduflex.DTOs.Department
{
    // Lightweight projection used to show a user's department memberships (e.g. on the
    // Users list) without pulling the whole DepartmentDto — see DepartmentMappingExtension.ToBadges.
    public class DepartmentBadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHead { get; set; }
    }
}
