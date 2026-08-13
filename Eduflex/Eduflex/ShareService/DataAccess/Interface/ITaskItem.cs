using ShareService.Common;
using ShareService.Models.Task;

namespace ShareService.DataAccess.Interface
{
    public interface ITaskItem
    {
        Task<bool> CreateTaskAsync(TaskItemModel task);
        Task<TaskItemModel?> GetTaskByIdAsync(string id);

        // restrictToInvolvedUserIds: when provided, only tasks where AssignerUserId OR
        // AssigneeUserId is in the list are returned — pass [userId] for "My Tasks", or a
        // department's member+head ids for the manager's "All Tasks" page. Pass null for a
        // linked-record Tasks tab search, where filter.EnrolmentId/etc. already scopes the
        // query and every involved user should show up regardless of who's looking.
        Task<PagedResult<TaskItemModel>> SearchTasksAsync(TaskItemFilter filter, List<string>? restrictToInvolvedUserIds);

        Task<bool> ReplaceTaskAsync(string id, TaskItemModel task);
    }
}
