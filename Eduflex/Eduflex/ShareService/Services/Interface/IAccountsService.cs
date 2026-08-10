using ShareService.Common;
using ShareService.Models.Accounts;

namespace ShareService.Services.Interface
{
    public interface IAccountsService
    {
        Task<ActionQueueResultModel> GetActionQueueAsync(int windowDays, string userId);

        Task<PagedResult<AccountSummaryModel>> GetPortfolioAsync(
            string? search, string? accountType, string? status, int pageNumber, int pageSize, string userId);

        Task<AccountTimelineModel> GetAccountTimelineAsync(string accountType, string accountKey, string userId);
    }
}
