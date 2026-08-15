using Eduflex.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using ShareService.Messaging;
using ShareService.Services.Interface;

namespace Eduflex.API.Realtime
{
    // The seam between ShareService (which can't reference this project's Hub type,
    // since Eduflex already references ShareService — a reference back would be
    // circular) and the actual SignalR hub connections that live here.
    public class SignalRNotificationBroadcaster : INotificationBroadcaster
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastAsync(NotificationMessage message)
        {
            var groups = message.RecipientUserIds.Select(NotificationHub.UserGroupName).ToList();
            if (groups.Count == 0)
            {
                return;
            }

            await _hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", message);
        }
    }
}
