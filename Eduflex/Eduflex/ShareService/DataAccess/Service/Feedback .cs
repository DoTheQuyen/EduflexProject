using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Feedback;

namespace ShareService.DataAccess
{
    public class Feedback : AuditableCollectionBase<FeedbackModel>, IFeedback
    {
        public Feedback(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<FeedbackModel>("Feedbacks"), currentUser)
        {
        }

        public async Task<bool> CreateFeedbackAsync(FeedbackModel feedback)
        {
            await InsertOneAsync(feedback);
            return true;
        }

        public async Task<List<FeedbackModel>> GetLatestFeedbackAsync(int count)
        {
            return await Collection
                .Find(FilterDefinition<FeedbackModel>.Empty)
                .SortByDescending(f => f.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public Task<PagedResult<FeedbackModel>> GetFeedbackAsync(PaginationQuery query)
        {
            var filter = BuildSearchFilter(query.SearchTerm, f => f.Name, f => f.CourseName);
            var sort = Builders<FeedbackModel>.Sort.Descending(f => f.CreatedAt);

            return GetPagedAsync(filter, sort, query.PageNumber, query.PageSize);
        }

        public async Task<bool> DeleteFeedbackAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
