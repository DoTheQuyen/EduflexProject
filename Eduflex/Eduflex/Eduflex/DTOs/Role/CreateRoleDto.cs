using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Role
{
    public class CreateRoleDto
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; } = new();
        public RoleTypeEnums roleType { get; set; }
    }
}
