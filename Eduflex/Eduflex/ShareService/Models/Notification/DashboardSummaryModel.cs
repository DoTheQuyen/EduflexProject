namespace ShareService.Models.Notification
{
    public class DashboardSummaryModel
    {
        public List<NotificationModel> Notifications { get; set; } = new();
        public Dictionary<string, int> Counts { get; set; } = new();
    }
}
