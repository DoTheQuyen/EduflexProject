using ShareService.Models.Notification;

namespace ShareService.DataAccess.Interface
{
    public interface INotification
    {
        Task<bool> CreateNotificationAsync(NotificationModel notification);
        Task<List<NotificationModel>> GetActiveNotificationsForUserAsync(string userId);
        Task<bool> ClearNotificationAsync(string id, string userId);
    }
}
