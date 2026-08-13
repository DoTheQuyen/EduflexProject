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
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IDashboardService dashboardService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _dashboardService = dashboardService;
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

        // Notifications + per-module open/actionable counts in one call, so the bell and
        // the sidebar/dashboard count bubbles never disagree and both refresh on the same
        // poll cycle instead of firing separate requests.
        [HttpGet("summary")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            return HandleRequestAsync(_logger, "Error in GetDashboardSummary endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var summary = await _dashboardService.GetDashboardSummaryAsync(userId);
                return summary.ToDto();
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
