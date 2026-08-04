using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Eduflex.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Recipients are now resolved per-notification (department members, a department
        // head, or an explicit staff list) rather than by a single role claim, so each
        // connection joins a group keyed by its own user id instead of its role — the
        // listener fans a message out to exactly the resolved recipient ids' groups.
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
            }

            await base.OnConnectedAsync();
        }

        public static string UserGroupName(string userId) => $"user:{userId}";
    }
}
