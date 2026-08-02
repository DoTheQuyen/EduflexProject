using System.Text.Json.Serialization;

namespace ShareService.Enums.Notification
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationTargetType
    {
        // Broadcast to every member of a department.
        Department,

        // Only the department's assigned head.
        DepartmentHead,

        // An explicit, caller-supplied list of user ids — for actions that should only
        // reach specific people rather than a whole department.
        Staff
    }
}
