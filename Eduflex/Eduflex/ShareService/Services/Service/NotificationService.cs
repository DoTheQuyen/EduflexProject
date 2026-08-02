using ShareService.DataAccess.Interface;
using ShareService.Models.Notification;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotification _notificationDataAccess;

        public NotificationService(INotification notificationDataAccess)
        {
            _notificationDataAccess = notificationDataAccess;
        }

        public Task<List<NotificationModel>> GetMyNotificationsAsync(string userId)
        {
            return _notificationDataAccess.GetActiveNotificationsForUserAsync(userId);
        }

        public Task<bool> ClearNotificationAsync(string id, string userId)
        {
            return _notificationDataAccess.ClearNotificationAsync(id, userId);
        }
    }
}
