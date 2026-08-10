using ShareService.Models.Role;

namespace ShareService.Mapping
{
    public static class RoleMappingExtension
    {
        public static void ApplyEditableFields(this RoleModel existing, RoleModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Description = updateModel.Description;
            existing.RoleType = updateModel.RoleType;
            existing.PermissionIds = updateModel.PermissionIds;
        }
    }
}