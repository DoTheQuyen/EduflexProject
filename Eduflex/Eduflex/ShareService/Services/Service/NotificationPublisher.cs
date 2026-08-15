using ShareService.DataAccess.Interface;
using ShareService.Enums.Notification;
using ShareService.Enums.Roles;
using ShareService.Messaging;
using ShareService.Models.Notification;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class NotificationPublisher : INotificationPublisher
    {
        private readonly INotification _notificationDataAccess;
        private readonly IDepartment _departmentDataAccess;
        private readonly IUserDB _userDB;
        private readonly IRoleService _roleService;
        private readonly INotificationBroadcaster _broadcaster;

        public NotificationPublisher(
            INotification notificationDataAccess,
            IDepartment departmentDataAccess,
            IUserDB userDB,
            IRoleService roleService,
            INotificationBroadcaster broadcaster)
        {
            _notificationDataAccess = notificationDataAccess;
            _departmentDataAccess = departmentDataAccess;
            _userDB = userDB;
            _roleService = roleService;
            _broadcaster = broadcaster;
        }

        public async Task PublishAsync(string module, string entityId, string summary, NotificationTarget target)
        {
            var recipientUserIds = await ResolveRecipientsAsync(target);
            if (recipientUserIds.Count == 0)
            {
                // Nobody to notify (e.g. a department with no members yet, or a head that
                // hasn't been assigned) — don't persist/broadcast an orphan notification.
                return;
            }

            var notification = new NotificationModel
            {
                Module = module,
                EntityId = entityId,
                Summary = summary,
                TargetType = target.Type.ToString(),
                TargetDepartmentId = target.DepartmentId,
                RecipientUserIds = recipientUserIds
            };
            // CreateNotificationAsync (via InsertOneAsync) assigns notification.Id in place —
            // the Mongo driver populates the generated ObjectId back onto this same object.
            await _notificationDataAccess.CreateNotificationAsync(notification);

            var message = new NotificationMessage(
                notification.Id, module, entityId, summary, target.Type.ToString(), target.DepartmentId, recipientUserIds);
            await _broadcaster.BroadcastAsync(message);
        }

        public async Task PublishToRoleAsync(string module, string entityId, string summary, SystemRole role)
        {
            var roleModel = await _roleService.GetByNameAsync(role.ToString());
            var userIds = roleModel == null
                ? new List<string>()
                : await _userDB.GetUserIdsByRoleIdAsync(roleModel.Id);

            await PublishAsync(module, entityId, summary, NotificationTarget.ToStaff(userIds));
        }

        private async Task<List<string>> ResolveRecipientsAsync(NotificationTarget target)
        {
            switch (target.Type)
            {
                case NotificationTargetType.Staff:
                    return target.UserIds ?? new List<string>();

                case NotificationTargetType.DepartmentHead:
                    {
                        var department = target.DepartmentId == null
                            ? null
                            : await _departmentDataAccess.GetDepartmentByIdAsync(target.DepartmentId);
                        return department?.HeadUserId == null
                            ? new List<string>()
                            : new List<string> { department.HeadUserId };
                    }

                case NotificationTargetType.Department:
                default:
                    {
                        var department = target.DepartmentId == null
                            ? null
                            : await _departmentDataAccess.GetDepartmentByIdAsync(target.DepartmentId);
                        return department?.MemberUserIds ?? new List<string>();
                    }
            }
        }
    }
}