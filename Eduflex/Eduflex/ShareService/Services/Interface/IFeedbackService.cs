using ShareService.Models.Feedback;

namespace ShareService.Services.Interface
{
    public interface IFeedbackService
    {
        Task<FeedbackModel> CreateFeedback(CreateFeedbackModel createDto);
        Task<List<FeedbackModel>> GetLatestFeedback(int count);
        Task<List<FeedbackModel>> GetAllFeedback();
        Task<bool> DeleteFeedback(string id);
    }
}