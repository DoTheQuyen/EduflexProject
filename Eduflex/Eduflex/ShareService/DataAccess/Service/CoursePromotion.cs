using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.CoursePromotion;

namespace ShareService.DataAccess
{
    public class CoursePromotion : AuditableCollectionBase<CoursePromotionModel>, ICoursePromotion
    {
        public CoursePromotion(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<CoursePromotionModel>("CoursePromotions"), currentUser)
        {
        }

        public async Task<bool> CreateCoursePromotionAsync(CoursePromotionModel promotion)
        {
            await InsertOneAsync(promotion);
            return true;
        }

        public async Task<CoursePromotionModel?> GetCoursePromotionByIdAsync(string id)
        {
            return await Collection
                .Find(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CoursePromotionModel>> GetFeaturedActiveCoursePromotionsAsync(int count)
        {
            var filter = Builders<CoursePromotionModel>.Filter.And(
                Builders<CoursePromotionModel>.Filter.Eq(p => p.IsFeatured, true),
                Builders<CoursePromotionModel>.Filter.Gte(p => p.ExpiryDate, DateTime.UtcNow));

            return await Collection
                .Find(filter)
                .SortBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .Limit(count)
                .ToListAsync();
        }

        public Task<PagedResult<CoursePromotionModel>> GetCoursePromotionsAsync(CoursePromotionFilter filter)
        {
            var mongoFilters = new List<FilterDefinition<CoursePromotionModel>>
            {
                BuildSearchFilter(filter.SearchTerm, p => p.CourseName, p => p.UniversityName)
            };

            if (filter.IsFeatured.HasValue)
            {
                mongoFilters.Add(Builders<CoursePromotionModel>.Filter.Eq(p => p.IsFeatured, filter.IsFeatured.Value));
            }

            var mongoFilter = Builders<CoursePromotionModel>.Filter.And(mongoFilters);

            var sort = Builders<CoursePromotionModel>.Sort
                .Ascending(p => p.DisplayOrder)
                .Descending(p => p.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> UpdateCoursePromotionAsync(string id, CoursePromotionModel promotion)
        {
            return await ReplaceOneAsync(p => p.Id == id, promotion);
        }

        public async Task<bool> DeleteCoursePromotionAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
