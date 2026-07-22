using Eduflex.DTOs.Role;
using ShareService.Models.Role;

namespace Eduflex.Mapping.Role
{
    public static class RoleMappingExtension
    {
        public static RoleDto ToDto(this RoleModel model)
        {
            return new RoleDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                PermissionIds = model.PermissionIds
            };
        }

        public static CreateRoleModel ToModel(this CreateRoleDto dto)
        {
            return new CreateRoleModel
            {
                Name = dto.Name,
                Description = dto.Description,
                PermissionIds = dto.PermissionIds
            };
        }
    }
}
