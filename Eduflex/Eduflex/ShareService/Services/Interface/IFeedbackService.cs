using ShareService.Common;
using ShareService.Models.Feedback;

namespace ShareService.Services.Interface
{
    public interface IFeedbackService
    {
        Task<bool> CreateFeedback(FeedbackModel feedback);
        Task<List<FeedbackModel>> GetLatestFeedback(int count);
        Task<PagedResult<FeedbackModel>> GetFeedback(PaginationQuery query);
        Task<bool> DeleteFeedback(string id);
    }
}
