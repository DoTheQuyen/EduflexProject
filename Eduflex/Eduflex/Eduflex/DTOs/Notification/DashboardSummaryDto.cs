namespace Eduflex.DTOs.Notification
{
    public class DashboardSummaryDto
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public Dictionary<string, int> Counts { get; set; } = new();
    }
}
