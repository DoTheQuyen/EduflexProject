using ShareService.Models.Task;

namespace ShareService.Mapping
{
    public static class TaskItemMappingExtension
    {
        // Only the fields an assigner can edit after creation — never Id/audit fields,
        // never AssigneeUserId (that's ReassignAsync's job) and never Status (that's
        // ChangeStatusAsync's job), so a plain "edit task details" call can't smuggle in
        // a reassignment or a status flip without going through their own note-recording
        // paths.
        public static void ApplyEditableFields(this TaskItemModel existing, TaskItemModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Description = updateModel.Description;
            existing.DueDateTime = updateModel.DueDateTime;
            existing.EnrolmentId = updateModel.EnrolmentId;
            existing.EnquiryId = updateModel.EnquiryId;
            existing.ApplicationId = updateModel.ApplicationId;
            existing.FinancialRecordId = updateModel.FinancialRecordId;
            existing.MigrationCaseId = updateModel.MigrationCaseId;
        }
    }
}
