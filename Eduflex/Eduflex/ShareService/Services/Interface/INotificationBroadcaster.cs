using ShareService.Messaging;

namespace ShareService.Services.Interface
{
    public interface INotificationBroadcaster
    {
        Task BroadcastAsync(NotificationMessage message);
    }
}
