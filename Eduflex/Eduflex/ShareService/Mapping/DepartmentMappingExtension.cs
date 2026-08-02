using ShareService.Models.Department;

namespace ShareService.Mapping
{
    public static class DepartmentMappingExtension
    {
        public static void ApplyEditableFields(this DepartmentModel existing, DepartmentModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Description = updateModel.Description;
            existing.ParentDepartmentId = updateModel.ParentDepartmentId;
            existing.HeadUserId = updateModel.HeadUserId;
            existing.MemberUserIds = updateModel.MemberUserIds;
        }
    }
}
