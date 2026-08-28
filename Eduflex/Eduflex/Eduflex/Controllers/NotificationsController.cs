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

        [HttpGet("my-notifications")]
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

        // Monthly Enquiry/Application/Enrolment counts for the dashboard trend chart — a
        // separate, lighter-polled route from "summary" (which the realtime bell polls
        // frequently); this one is fetched once per dashboard load / period change.
        [HttpGet("monthly-trends")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<MonthlyTrendDto>> GetMonthlyTrends([FromQuery] int months = 6)
        {
            return HandleRequestAsync(_logger, "Error in GetMonthlyTrends endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var trends = await _dashboardService.GetMonthlyTrendsAsync(userId, months);
                return trends.ToDto();
            });
        }

        // Current pipeline composition per module (Enquiry/Application/Enrolment/
        // MigrationCase status counts) — the "where do things stand today" companion to
        // monthly-trends' "how much came in over time".
        [HttpGet("status-breakdown")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<StatusBreakdownDto>> GetStatusBreakdown()
        {
            return HandleRequestAsync(_logger, "Error in GetStatusBreakdown endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var breakdown = await _dashboardService.GetStatusBreakdownAsync(userId);
                return breakdown.ToDto();
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
