using ShareService.Common;
using ShareService.Enums.Task;
using ShareService.Models.Task;

namespace ShareService.Services.Interface
{
    public interface ITaskItemService
    {
        // My Tasks — every task where userId is the assigner or the current assignee.
        Task<PagedResult<TaskItemModel>> SearchMyTasksAsync(TaskItemFilter filter, string userId);

        // All Tasks — Manager/Admin only, scoped to the department(s) userId heads.
        Task<PagedResult<TaskItemModel>> SearchAllTasksAsync(TaskItemFilter filter, string userId);

        // Tasks tab on an Enrolment/Enquiry/Application/FinancialRecord detail page —
        // filter must already carry exactly one of the four linked-record ids.
        Task<PagedResult<TaskItemModel>> SearchLinkedTasksAsync(TaskItemFilter filter, string userId);

        Task<TaskItemModel?> GetTaskByIdAsync(string id, string userId);
        Task<bool> CreateTaskAsync(TaskItemModel task, string userId);
        Task<bool> UpdateTaskAsync(string id, TaskItemModel task, string userId);
        Task<TaskItemModel> AddNoteAsync(string id, string content, string userId);
        Task<bool> ReassignTaskAsync(string id, string newAssigneeUserId, string note, string userId);
        Task<bool> ChangeStatusAsync(string id, TaskItemStatus newStatus, string userId);
    }
}
