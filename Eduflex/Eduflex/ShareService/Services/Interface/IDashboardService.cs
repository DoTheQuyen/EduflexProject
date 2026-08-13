using ShareService.Models.Notification;

namespace ShareService.Services.Interface
{
    public interface IDashboardService
    {
        Task<DashboardSummaryModel> GetDashboardSummaryAsync(string userId);
    }
}
