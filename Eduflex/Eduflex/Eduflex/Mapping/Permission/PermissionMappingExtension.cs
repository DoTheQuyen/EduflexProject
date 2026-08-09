using Eduflex.DTOs.Permission;
using ShareService.Models.Auth;
using ShareService.Models.Permission;

namespace Eduflex.Mapping.Permission
{
    public static class PermissionMappingExtension
    {
        public static PermissionDto ToDto(this PermissionModel model, ModuleModel? module)
        {
            return new PermissionDto
            {
                Id = model.Id,
                Key = model.Key,
                Action = model.Action.ToString(),
                ModuleId = model.ModuleId,
                ModuleName = module?.Name ?? string.Empty,
                Description = model.Description
            };
        }
    }
}
