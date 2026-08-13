using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Enums.Task;
using ShareService.Mapping;
using ShareService.Messaging;
using ShareService.Models.Auth;
using ShareService.Models.Task;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class TaskItemService : ITaskItemService
    {
        private readonly ITaskItem _taskDataAccess;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IDepartmentService _departmentService;
        private readonly IEnrolment _enrolmentDataAccess;
        private readonly IEnquiry _enquiryDataAccess;
        private readonly IApplication _applicationDataAccess;
        private readonly IFinancialRecord _financialRecordDataAccess;
        private readonly IMigrationCase _migrationCaseDataAccess;
        private readonly IValidator<TaskItemModel> _taskValidator;
        private readonly IPermissionService _permissionService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger<TaskItemService> _logger;

        public TaskItemService(
            ITaskItem taskDataAccess,
            IUserService userService,
            IRoleService roleService,
            IDepartmentService departmentService,
            IEnrolment enrolmentDataAccess,
            IEnquiry enquiryDataAccess,
            IApplication applicationDataAccess,
            IFinancialRecord financialRecordDataAccess,
            IMigrationCase migrationCaseDataAccess,
            IValidator<TaskItemModel> taskValidator,
            IPermissionService permissionService,
            INotificationPublisher notificationPublisher,
            ILogger<TaskItemService> logger)
        {
            _taskDataAccess = taskDataAccess;
            _userService = userService;
            _roleService = roleService;
            _departmentService = departmentService;
            _enrolmentDataAccess = enrolmentDataAccess;
            _enquiryDataAccess = enquiryDataAccess;
            _applicationDataAccess = applicationDataAccess;
            _financialRecordDataAccess = financialRecordDataAccess;
            _migrationCaseDataAccess = migrationCaseDataAccess;
            _taskValidator = taskValidator;
            _permissionService = permissionService;
            _notificationPublisher = notificationPublisher;
            _logger = logger;
        }

        private async Task<List<string>> GetUserPermissionsAsync(string userId) =>
            await _permissionService.GetPermissionsForUserAsync(userId);

        private static bool HasPermission(List<string> permissions, PermissionKey key) =>
            permissions.Contains(key.GetDescription());

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (!HasPermission(permissions, key))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // Server-side enforcement, independent of what the UI happens to offer — mirrors
        // DepartmentService.CheckNoStudentMembersAsync, just for a single user id instead
        // of a list.
        private async Task CheckAssigneeIsStaffAsync(UserModel assignee)
        {
            var role = await _roleService.GetByIdAsync(assignee.RoleId);
            if (role?.RoleType == RoleTypeEnums.Student)
            {
                throw new ArgumentException("Tasks cannot be assigned to a student.");
            }
        }

        private async Task ValidateLinkedRecordsAsync(TaskItemModel task)
        {
            if (!string.IsNullOrEmpty(task.EnrolmentId) && await _enrolmentDataAccess.GetEnrolmentAsync(task.EnrolmentId) == null)
                throw new ArgumentException("Linked enrolment not found");

            if (!string.IsNullOrEmpty(task.EnquiryId) && await _enquiryDataAccess.GetEnquiryAsync(task.EnquiryId) == null)
                throw new ArgumentException("Linked enquiry not found");

            if (!string.IsNullOrEmpty(task.ApplicationId) && await _applicationDataAccess.GetApplicationByIdAsync(task.ApplicationId) == null)
                throw new ArgumentException("Linked application not found");

            if (!string.IsNullOrEmpty(task.FinancialRecordId) && await _financialRecordDataAccess.GetByIdAsync(task.FinancialRecordId) == null)
                throw new ArgumentException("Linked financial record not found");

            if (!string.IsNullOrEmpty(task.MigrationCaseId) && await _migrationCaseDataAccess.GetCaseAsync(task.MigrationCaseId) == null)
                throw new ArgumentException("Linked migration case not found");
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Unknown";
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId;
        }

        // Auth: requires TasksView — every staff member can see tasks they're involved in.
        public async Task<PagedResult<TaskItemModel>> SearchMyTasksAsync(TaskItemFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.TasksView, "view tasks");
            return await _taskDataAccess.SearchTasksAsync(filter, new List<string> { userId });
        }

        // Auth: requires TasksViewAll — Manager/Admin, scoped to the department(s) this
        // user heads. A Manager who heads no department sees an empty page rather than an
        // error, same shape as an empty search result elsewhere in the app.
        public async Task<PagedResult<TaskItemModel>> SearchAllTasksAsync(TaskItemFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.TasksViewAll, "view all department tasks");

            var managedDepartments = await _departmentService.GetDepartmentsManagedByUserAsync(userId);
            if (managedDepartments.Count == 0)
            {
                return new PagedResult<TaskItemModel>
                {
                    Items = new List<TaskItemModel>(),
                    TotalCount = 0,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };
            }

            var involvedUserIds = managedDepartments
                .SelectMany(d => d.MemberUserIds.Append(d.HeadUserId ?? string.Empty))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            return await _taskDataAccess.SearchTasksAsync(filter, involvedUserIds);
        }

        // Auth: requires TasksView. filter must already carry exactly one linked-record
        // id — enforced here so a caller can't accidentally list every task in the system
        // by posting an empty filter to this endpoint.
        public async Task<PagedResult<TaskItemModel>> SearchLinkedTasksAsync(TaskItemFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.TasksView, "view tasks");

            if (string.IsNullOrEmpty(filter.EnrolmentId) && string.IsNullOrEmpty(filter.EnquiryId) &&
                string.IsNullOrEmpty(filter.ApplicationId) && string.IsNullOrEmpty(filter.FinancialRecordId) &&
                string.IsNullOrEmpty(filter.MigrationCaseId))
            {
                throw new ArgumentException("A linked record id is required");
            }

            return await _taskDataAccess.SearchTasksAsync(filter, null);
        }

        // Auth: requires TasksView, plus either being the assigner/assignee or holding
        // TasksViewAll (a Manager/Admin can open a task link even outside the department
        // they head — same broad-admin-key shape as GetDepartmentByIdAsync not narrowing
        // further than DepartmentsView).
        public async Task<TaskItemModel?> GetTaskByIdAsync(string id, string userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (!HasPermission(permissions, PermissionKey.TasksView))
            {
                throw new UnauthorizedAccessException("You do not have permission to view tasks");
            }

            var task = await _taskDataAccess.GetTaskByIdAsync(id) ?? throw new KeyNotFoundException("Task not found");

            var isInvolved = task.AssignerUserId == userId || task.AssigneeUserId == userId;
            if (!isInvolved && !HasPermission(permissions, PermissionKey.TasksViewAll))
            {
                throw new UnauthorizedAccessException("You can only view tasks you are the assigner or assignee of.");
            }

            return task;
        }

        // Auth: requires TasksAdd. The creator becomes AssignerUserId regardless of what
        // the incoming model says — never trust the caller for that field.
        public async Task<bool> CreateTaskAsync(TaskItemModel task, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.TasksAdd, "create tasks");

                var assignee = await _userService.GetUserByIdAsync(task.AssigneeUserId)
                    ?? throw new ArgumentException("The selected assignee was not found");
                await CheckAssigneeIsStaffAsync(assignee);

                task.Id = string.Empty;
                task.AssignerUserId = userId;
                task.Status = TaskItemStatus.New.ToString();

                var validate = await _taskValidator.ValidateAsync(task);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                await ValidateLinkedRecordsAsync(task);

                var assignerName = await ResolveUserNameAsync(userId);
                var assigneeName = $"{assignee.FirstName} {assignee.LastName}".Trim();
                task.Notes = new List<TaskNoteModel>
                {
                    TaskNoteModel.Create(TaskNoteType.StatusChange, $"Task created and assigned to {assigneeName} by {assignerName}", userId, assignerName)
                };

                var created = await _taskDataAccess.CreateTaskAsync(task);

                if (created)
                {
                    await _notificationPublisher.PublishAsync(
                        "Task", task.Id, $"New task \"{task.Name}\" assigned to you by {assignerName}",
                        NotificationTarget.ToStaff(new[] { task.AssigneeUserId }));
                }

                return created;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task: {TaskName}", task.Name);
                throw;
            }
        }

        // Auth: requires TasksEdit + must be the assigner. Read-only once Completed —
        // reopen it (ChangeStatusAsync back to Processing) before editing details.
        public async Task<bool> UpdateTaskAsync(string id, TaskItemModel task, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.TasksEdit, "edit tasks");

                var existing = await _taskDataAccess.GetTaskByIdAsync(id) ?? throw new KeyNotFoundException("Task not found");

                if (existing.AssignerUserId != userId)
                {
                    throw new UnauthorizedAccessException("Only this task's assigner can edit its details.");
                }

                if (existing.Status == TaskItemStatus.Completed.ToString())
                {
                    throw new ArgumentException("This task is completed and read-only. Reopen it first.");
                }

                task.Id = id;
                task.AssigneeUserId = existing.AssigneeUserId;

                var validate = await _taskValidator.ValidateAsync(task);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                await ValidateLinkedRecordsAsync(task);

                existing.ApplyEditableFields(task);

                return await _taskDataAccess.ReplaceTaskAsync(existing.Id, existing);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {TaskId}", id);
                throw;
            }
        }

        // Auth: requires TasksView + must be the assigner or the current assignee. Either
        // side of the conversation can add a note at any time, including on a completed
        // (read-only-to-edit) task — notes are the one thing that stays open regardless of
        // status, since "why was this reopened" etc. needs to be recordable too.
        public async Task<TaskItemModel> AddNoteAsync(string id, string content, string userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (!HasPermission(permissions, PermissionKey.TasksView))
            {
                throw new UnauthorizedAccessException("You do not have permission to view tasks");
            }

            var existing = await _taskDataAccess.GetTaskByIdAsync(id) ?? throw new KeyNotFoundException("Task not found");

            if (existing.AssignerUserId != userId && existing.AssigneeUserId != userId)
            {
                throw new UnauthorizedAccessException("Only this task's assigner or assignee can add a note.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Note content is required");
            }

            var actingUserName = await ResolveUserNameAsync(userId);
            existing.Notes.Add(TaskNoteModel.Create(TaskNoteType.Note, content, userId, actingUserName));

            await _taskDataAccess.ReplaceTaskAsync(existing.Id, existing);
            return existing;
        }

        // Auth: requires TasksView + must be the assigner or the current assignee — either
        // side can hand a task off to someone else, per the module spec ("assignee can
        // respond, or reassign to someone with note"). A note is mandatory and recorded as
        // its own timeline entry type (TaskNoteType.Reassign), separate from a manual note.
        public async Task<bool> ReassignTaskAsync(string id, string newAssigneeUserId, string note, string userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (!HasPermission(permissions, PermissionKey.TasksView))
            {
                throw new UnauthorizedAccessException("You do not have permission to view tasks");
            }

            var existing = await _taskDataAccess.GetTaskByIdAsync(id) ?? throw new KeyNotFoundException("Task not found");

            if (existing.AssignerUserId != userId && existing.AssigneeUserId != userId)
            {
                throw new UnauthorizedAccessException("Only this task's assigner or assignee can reassign it.");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                throw new ArgumentException("A note is required when reassigning a task");
            }

            var newAssignee = await _userService.GetUserByIdAsync(newAssigneeUserId)
                ?? throw new ArgumentException("The selected assignee was not found");
            await CheckAssigneeIsStaffAsync(newAssignee);

            var previousAssigneeName = await ResolveUserNameAsync(existing.AssigneeUserId);
            var newAssigneeName = $"{newAssignee.FirstName} {newAssignee.LastName}".Trim();
            var actingUserName = await ResolveUserNameAsync(userId);

            existing.AssigneeUserId = newAssigneeUserId;
            existing.Notes.Add(TaskNoteModel.Create(
                TaskNoteType.Reassign,
                $"Reassigned from {previousAssigneeName} to {newAssigneeName}: {note}",
                userId, actingUserName));

            var saved = await _taskDataAccess.ReplaceTaskAsync(existing.Id, existing);

            if (saved)
            {
                await _notificationPublisher.PublishAsync(
                    "Task", id, $"Task \"{existing.Name}\" reassigned to you by {actingUserName}",
                    NotificationTarget.ToStaff(new[] { newAssigneeUserId }));
            }

            return saved;
        }

        // Auth: requires TasksView + must be the assigner or the current assignee. Allowed
        // transitions only: New/Processing -> Completed, and Completed -> Processing
        // (reopen). Every transition is recorded as its own timeline entry — including the
        // reopen, per the module spec ("can change back to processing, but will record as
        // note when status change").
        public async Task<bool> ChangeStatusAsync(string id, TaskItemStatus newStatus, string userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (!HasPermission(permissions, PermissionKey.TasksView))
            {
                throw new UnauthorizedAccessException("You do not have permission to view tasks");
            }

            var existing = await _taskDataAccess.GetTaskByIdAsync(id) ?? throw new KeyNotFoundException("Task not found");

            if (existing.AssignerUserId != userId && existing.AssigneeUserId != userId)
            {
                throw new UnauthorizedAccessException("Only this task's assigner or assignee can change its status.");
            }

            var currentStatus = Enum.Parse<TaskItemStatus>(existing.Status);
            var validTransition =
                (newStatus == TaskItemStatus.Processing && currentStatus is TaskItemStatus.New or TaskItemStatus.Completed) ||
                (newStatus == TaskItemStatus.Completed && currentStatus is TaskItemStatus.New or TaskItemStatus.Processing);

            if (!validTransition)
            {
                throw new ArgumentException($"Cannot change status from {currentStatus} to {newStatus}");
            }

            var actingUserName = await ResolveUserNameAsync(userId);
            var description = currentStatus == TaskItemStatus.Completed
                ? $"Reopened — status changed back to Processing by {actingUserName}"
                : $"Status changed from {currentStatus} to {newStatus} by {actingUserName}";

            existing.Status = newStatus.ToString();
            existing.Notes.Add(TaskNoteModel.Create(TaskNoteType.StatusChange, description, userId, actingUserName));

            return await _taskDataAccess.ReplaceTaskAsync(existing.Id, existing);
        }
    }
}
