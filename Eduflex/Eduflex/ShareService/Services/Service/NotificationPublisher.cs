using ShareService.DataAccess.Interface;
using ShareService.Enums.Notification;
using ShareService.Enums.Roles;
using ShareService.Messaging;
using ShareService.Models.Notification;
using ShareService.Services.Interface;
using StackExchange.Redis;
using System.Text.Json;

namespace ShareService.Services
{
    public class NotificationPublisher : INotificationPublisher
    {
        private readonly INotification _notificationDataAccess;
        private readonly IDepartment _departmentDataAccess;
        private readonly IUserDB _userDB;
        private readonly IRoleService _roleService;
        // Redis pub/sub disabled (cost) — see Program.cs. Notifications still persist to
        // Mongo via CreateNotificationAsync below; only the live SignalR push (fed by
        // NotificationListener subscribing to this channel) stops. Uncomment this field,
        // the constructor parameter below, and the publish block in PublishAsync —
        // together with Program.cs's IConnectionMultiplexer/NotificationListener
        // registrations — to restore it.
        // private readonly IConnectionMultiplexer _redisMultiplexer;

        public NotificationPublisher(
            INotification notificationDataAccess,
            IDepartment departmentDataAccess,
            IUserDB userDB,
            IRoleService roleService)
        // IConnectionMultiplexer redisMultiplexer)
        {
            _notificationDataAccess = notificationDataAccess;
            _departmentDataAccess = departmentDataAccess;
            _userDB = userDB;
            _roleService = roleService;
            // _redisMultiplexer = redisMultiplexer;
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

            // Redis pub/sub disabled (cost) — notification is already persisted above via
            // CreateNotificationAsync; RealtimeNotificationService.loadBacklog() on the
            // frontend fetches it from there on connect/page load. Only the live push to a
            // client that's already on the page while this happens is skipped.
            // var message = new NotificationMessage(
            //     notification.Id, module, entityId, summary, target.Type.ToString(), target.DepartmentId, recipientUserIds);
            // var payload = JsonSerializer.Serialize(message);
            // await _redisMultiplexer.GetSubscriber().PublishAsync(RedisChannel.Literal(RedisChannels.Notifications), payload);
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