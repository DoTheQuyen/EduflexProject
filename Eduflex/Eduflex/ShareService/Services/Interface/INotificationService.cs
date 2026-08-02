using ShareService.Models.Notification;

namespace ShareService.Services.Interface
{
    public interface INotificationService
    {
        Task<List<NotificationModel>> GetMyNotificationsAsync(string userId);
        Task<bool> ClearNotificationAsync(string id, string userId);
    }
}
