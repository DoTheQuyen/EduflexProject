using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Feedback;

namespace ShareService.DataAccess
{
    public class Feedback : IFeedback
    {
        private readonly IMongoCollection<FeedbackModel> _feedbackCollection;

        public Feedback(IMongoDatabase database)
        {
            _feedbackCollection = database.GetCollection<FeedbackModel>("Feedbacks");
        }

        public async Task<FeedbackModel> CreateFeedbackAsync(FeedbackModel feedback)
        {
            await _feedbackCollection.InsertOneAsync(feedback);
            return feedback;
        }

        public async Task<List<FeedbackModel>> GetLatestFeedbackAsync(int count)
        {
            return await _feedbackCollection
                .Find(FilterDefinition<FeedbackModel>.Empty)
                .SortByDescending(f => f.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<List<FeedbackModel>> GetAllFeedbackAsync()
        {
            return await _feedbackCollection
                .Find(FilterDefinition<FeedbackModel>.Empty)
                .SortByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteFeedbackAsync(string id)
        {
            var result = await _feedbackCollection.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }
    }
}