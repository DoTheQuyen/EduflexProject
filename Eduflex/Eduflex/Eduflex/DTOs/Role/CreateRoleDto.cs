namespace Eduflex.DTOs.Role
{
    public class CreateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; } = new();
    }
}
