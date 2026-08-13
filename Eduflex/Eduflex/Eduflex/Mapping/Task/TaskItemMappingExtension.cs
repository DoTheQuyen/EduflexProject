using Eduflex.DTOs.Task;
using ShareService.Enums.Task;
using ShareService.Models.Task;

namespace Eduflex.Mapping.Task
{
    public static class TaskItemMappingExtension
    {
        public static TaskItemFilter ToFilter(this TaskItemFilterDto dto)
        {
            TaskItemStatus? status = null;
            if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<TaskItemStatus>(dto.Status, out var parsed))
            {
                status = parsed;
            }

            TaskItemStatus? excludeStatus = null;
            if (!string.IsNullOrEmpty(dto.ExcludeStatus) && Enum.TryParse<TaskItemStatus>(dto.ExcludeStatus, out var parsedExclude))
            {
                excludeStatus = parsedExclude;
            }

            return new TaskItemFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm,
                Status = status,
                ExcludeStatus = excludeStatus,
                EnrolmentId = dto.EnrolmentId,
                EnquiryId = dto.EnquiryId,
                ApplicationId = dto.ApplicationId,
                FinancialRecordId = dto.FinancialRecordId,
                MigrationCaseId = dto.MigrationCaseId
            };
        }

        public static TaskNoteDto ToDto(this TaskNoteModel model)
        {
            return new TaskNoteDto
            {
                Id = model.Id,
                Type = model.Type,
                Content = model.Content,
                CreatedByUserId = model.CreatedByUserId,
                CreatedByName = model.CreatedByName,
                CreatedAt = model.CreatedAt
            };
        }

        public static TaskItemDto ToDto(this TaskItemModel model)
        {
            return new TaskItemDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                AssignerUserId = model.AssignerUserId,
                AssigneeUserId = model.AssigneeUserId,
                DueDateTime = model.DueDateTime,
                Status = model.Status,
                EnrolmentId = model.EnrolmentId,
                EnquiryId = model.EnquiryId,
                ApplicationId = model.ApplicationId,
                FinancialRecordId = model.FinancialRecordId,
                MigrationCaseId = model.MigrationCaseId,
                Notes = model.Notes.Select(n => n.ToDto()).ToList(),
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        public static TaskItemModel ToModel(this CreateTaskItemDto dto)
        {
            return new TaskItemModel
            {
                Name = dto.Name,
                Description = dto.Description,
                AssigneeUserId = dto.AssigneeUserId,
                DueDateTime = dto.DueDateTime,
                EnrolmentId = dto.EnrolmentId,
                EnquiryId = dto.EnquiryId,
                ApplicationId = dto.ApplicationId,
                FinancialRecordId = dto.FinancialRecordId,
                MigrationCaseId = dto.MigrationCaseId
            };
        }

        // AssigneeUserId intentionally left blank — TaskItemService.UpdateTaskAsync
        // carries the existing assignee over onto this model before validating/applying,
        // since ApplyEditableFields never touches it anyway.
        public static TaskItemModel ToModel(this UpdateTaskItemDto dto)
        {
            return new TaskItemModel
            {
                Name = dto.Name,
                Description = dto.Description,
                DueDateTime = dto.DueDateTime,
                EnrolmentId = dto.EnrolmentId,
                EnquiryId = dto.EnquiryId,
                ApplicationId = dto.ApplicationId,
                FinancialRecordId = dto.FinancialRecordId,
                MigrationCaseId = dto.MigrationCaseId
            };
        }
    }
}
