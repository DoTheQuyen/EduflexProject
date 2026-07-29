using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Course;

namespace ShareService.DataAccess
{
    public class Course : AuditableCollectionBase<CourseModel>, ICourse
    {
        public Course(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<CourseModel>("Courses"), currentUser)
        {
        }

        public async Task<bool> CreateCourseAsync(CourseModel course)
        {
            await InsertOneAsync(course);
            return true;
        }

        public async Task<CourseModel?> GetCourseByIdAsync(string id)
        {
            return await Collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<CourseModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<CourseModel>.Empty).ToListAsync();
        }

        public async Task<List<CourseModel>> GetByPartnerIdAsync(string partnerId)
        {
            return await Collection.Find(c => c.EducationPartnerId == partnerId).ToListAsync();
        }

        public async Task<List<CourseModel>> GetByPartnerIdsAsync(IEnumerable<string> partnerIds)
        {
            var idList = partnerIds.ToList();
            if (!idList.Any())
            {
                return new List<CourseModel>();
            }

            return await Collection.Find(c => idList.Contains(c.EducationPartnerId)).ToListAsync();
        }

        public async Task<bool> UpdateCourseAsync(string id, CourseModel course)
        {
            return await ReplaceOneAsync(c => c.Id == id, course);
        }

        public async Task<bool> DeleteCourseAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(c => c.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task DeleteByPartnerIdAsync(string partnerId)
        {
            await Collection.DeleteManyAsync(c => c.EducationPartnerId == partnerId);
        }
    }
}
