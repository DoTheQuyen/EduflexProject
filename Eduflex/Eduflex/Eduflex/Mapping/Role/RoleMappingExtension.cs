using Eduflex.DTOs.Role;
using ShareService.Common;
using ShareService.Models.Role;

namespace Eduflex.Mapping.Role
{
    public static class RoleMappingExtension
    {
        public static PaginationQuery ToFilter(this RoleFilterDto dto)
        {
            return new PaginationQuery
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

        public static RoleDto ToDto(this RoleModel model)
        {
            return new RoleDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                roleType = model.RoleType,
                PermissionIds = model.PermissionIds,
                UserCount = model.UserCount
            };
        }

        public static RoleModel ToModel(this CreateRoleDto dto)
        {
            return new RoleModel
            {
                Name = dto.Name,
                Description = dto.Description,
                RoleType = dto.roleType,
                PermissionIds = dto.PermissionIds
            };
        }

        public static RoleSummaryDto ToSummaryDto(this RoleModel model)
        {
            return new RoleSummaryDto
            {
                Id = model.Id,
                Name = model.Name,
                RoleType = model.RoleType
            };
        }
    }
}