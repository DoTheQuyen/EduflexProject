using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.CoursePromotion;

namespace ShareService.DataAccess
{
    public class CoursePromotion : ICoursePromotion
    {
        private readonly IMongoCollection<CoursePromotionModel> _coursePromotionCollection;

        public CoursePromotion(IMongoDatabase database)
        {
            _coursePromotionCollection = database.GetCollection<CoursePromotionModel>("CoursePromotions");
        }

        public async Task<CoursePromotionModel> CreateCoursePromotionAsync(CoursePromotionModel promotion)
        {
            await _coursePromotionCollection.InsertOneAsync(promotion);
            return promotion;
        }

        public async Task<CoursePromotionModel?> GetCoursePromotionByIdAsync(string id)
        {
            return await _coursePromotionCollection
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotionsAsync(int count)
        {
            var filter = Builders<CoursePromotionModel>.Filter.And(
                Builders<CoursePromotionModel>.Filter.Eq(p => p.IsFeatured, true),
                Builders<CoursePromotionModel>.Filter.Gte(p => p.ExpiryDate, DateTime.UtcNow));

            return await _coursePromotionCollection
                .Find(filter)
                .SortBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<List<CoursePromotionModel>> GetAllCoursePromotionsAsync()
        {
            return await _coursePromotionCollection
                .Find(FilterDefinition<CoursePromotionModel>.Empty)
                .SortBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<CoursePromotionModel?> UpdateCoursePromotionAsync(string id, CoursePromotionModel promotion)
        {
            var result = await _coursePromotionCollection.FindOneAndReplaceAsync(
                p => p.Id == id,
                promotion,
                new FindOneAndReplaceOptions<CoursePromotionModel> { ReturnDocument = ReturnDocument.After });
            return result;
        }

        public async Task<bool> DeleteCoursePromotionAsync(string id)
        {
            var result = await _coursePromotionCollection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
