using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Role
{
    public class RoleDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public RoleTypeEnums? roleType { get; set; }
        public List<string> PermissionIds { get; set; } = new();
        public int UserCount { get; set; }
    }
}