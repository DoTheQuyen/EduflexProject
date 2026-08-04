using ShareService.Enums.Roles;
using ShareService.Messaging;

namespace ShareService.Services.Interface
{
    public interface INotificationPublisher
    {
        Task PublishAsync(string module, string entityId, string summary, NotificationTarget target);

        // TODO(department-migration): temporary compatibility shim so existing callers
        // (Enquiry, Application, Enrolment, FinancialRecord, Feedback) keep notifying
        // exactly who they notify today — everyone currently holding this SystemRole —
        // while the app migrates to department-based targeting. Once you've decided which
        // department each of those call sites actually belongs to, replace the call with
        // PublishAsync(..., NotificationTarget.ToDepartment(id)) or ToDepartmentHead(id)
        // directly, and delete this method once nothing calls it anymore.
        Task PublishToRoleAsync(string module, string entityId, string summary, SystemRole role);
    }
}
