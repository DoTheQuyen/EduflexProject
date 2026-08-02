using ShareService.Enums.Notification;

namespace ShareService.Messaging
{
    // Describes *who* a notification is for, before recipient ids are resolved.
    // NotificationPublisher.PublishAsync resolves this into a concrete RecipientUserIds
    // snapshot at send time — membership changes after a notification is sent don't
    // retroactively add/remove recipients from it.
    public class NotificationTarget
    {
        public NotificationTargetType Type { get; }
        public string? DepartmentId { get; }
        public List<string>? UserIds { get; }

        private NotificationTarget(NotificationTargetType type, string? departmentId, List<string>? userIds)
        {
            Type = type;
            DepartmentId = departmentId;
            UserIds = userIds;
        }

        public static NotificationTarget ToDepartment(string departmentId) =>
            new(NotificationTargetType.Department, departmentId, null);

        public static NotificationTarget ToDepartmentHead(string departmentId) =>
            new(NotificationTargetType.DepartmentHead, departmentId, null);

        public static NotificationTarget ToStaff(IEnumerable<string> userIds) =>
            new(NotificationTargetType.Staff, null, userIds.ToList());
    }
}
