using ShareService.Models.Auth;

namespace ShareService.Mapping
{
    public static class UserMappingExtension
    {
        public static void ApplyEditableFields(this UserModel existing, UserModel updateModel)
        {
            existing.Email = updateModel.Email;
            existing.FirstName = updateModel.FirstName;
            existing.MiddleName = updateModel.MiddleName;
            existing.LastName = updateModel.LastName;
            existing.Mobile = updateModel.Mobile;
            existing.RoleId = updateModel.RoleId;
            existing.IsActive = updateModel.IsActive;
        }

    }
}
