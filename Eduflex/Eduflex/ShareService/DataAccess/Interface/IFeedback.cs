using ShareService.Models.Feedback;

namespace ShareService.DataAccess.Interface
{
    public interface IFeedback
    {
        Task<FeedbackModel> CreateFeedbackAsync(FeedbackModel feedback);
        Task<List<FeedbackModel>> GetLatestFeedbackAsync(int count);
        Task<List<FeedbackModel>> GetAllFeedbackAsync();
        Task<bool> DeleteFeedbackAsync(string id);
    }
}