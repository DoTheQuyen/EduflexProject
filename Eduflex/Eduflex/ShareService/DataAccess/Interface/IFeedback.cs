using ShareService.Common;
using ShareService.Models.Feedback;

namespace ShareService.DataAccess.Interface
{
    public interface IFeedback
    {
        Task<bool> CreateFeedbackAsync(FeedbackModel feedback);
        Task<List<FeedbackModel>> GetLatestFeedbackAsync(int count);
        Task<PagedResult<FeedbackModel>> GetFeedbackAsync(PaginationQuery query);
        Task<bool> DeleteFeedbackAsync(string id);
    }
}
