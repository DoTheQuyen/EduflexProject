namespace Eduflex.DTOs.Role
{
    public class RoleDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; } = new();
    }
}
