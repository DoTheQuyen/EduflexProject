using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Role
{
    public class RoleSummaryDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public RoleTypeEnums? RoleType { get; set; }
    }
}