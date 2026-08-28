using ShareService.Models.Notification;

namespace ShareService.Services.Interface
{
    public interface IDashboardService
    {
        Task<DashboardSummaryModel> GetDashboardSummaryAsync(string userId);
        Task<MonthlyTrendModel> GetMonthlyTrendsAsync(string userId, int months = 6);
        Task<StatusBreakdownModel> GetStatusBreakdownAsync(string userId);
    }
}
