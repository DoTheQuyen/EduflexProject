using Eduflex.DTOs.Notification;
using Eduflex.Mapping.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<NotificationDto>>> GetMyNotifications()
        {
            return HandleRequestAsync(_logger, "Error in GetMyNotifications endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var notifications = await _notificationService.GetMyNotificationsAsync(userId);
                return notifications.Select(n => n.ToDto()).ToList();
            });
        }

        [HttpPatch("{id}/clear")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> ClearNotification(string id)
        {
            return HandleUpdateAsync(_logger, "Error in ClearNotification endpoint", () =>
            {
                var userId = GetRequiredUserId();
                return _notificationService.ClearNotificationAsync(id, userId);
            });
        }
    }
}
