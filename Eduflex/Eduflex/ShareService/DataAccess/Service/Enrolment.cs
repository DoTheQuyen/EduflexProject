using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enrolment;

namespace ShareService.DataAccess
{
    public class Enrolment : AuditableCollectionBase<EnrolmentModel>, IEnrolment
    {
        public Enrolment(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<EnrolmentModel>("Enrolments"), currentUser)
        {
        }

        public async Task<bool> CreateEnrolmentAsync(EnrolmentModel enrolment, IClientSessionHandle? session = null)
        {
            await InsertOneAsync(enrolment, session);
            return true;
        }

        public async Task<EnrolmentModel?> GetEnrolmentAsync(string id)
        {
            return await Collection
                .Find(e => e.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<EnrolmentModel?> GetEnrolmentByEnquiryIdAsync(string enquiryId)
        {
            return await Collection
                .Find(e => e.EnquiryId == enquiryId)
                .FirstOrDefaultAsync();
        }

        // Used by the student-facing Dynamic Forms endpoints — a student's Application
        // module only knows its own applicationId, not the linked Enrolment's id.
        public async Task<EnrolmentModel?> GetEnrolmentByStudentApplicationIdAsync(string studentApplicationId)
        {
            return await Collection
                .Find(e => e.StudentApplicationId == studentApplicationId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<EnrolmentModel>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return new List<EnrolmentModel>();
            }

            return await Collection.Find(e => idList.Contains(e.Id)).ToListAsync();
        }

        // Used by the Student Details page's history panel and by student deactivation's
        // document-purge step — a student may have more than one enrolment over time,
        // ordered oldest first so it reads as a timeline.
        public async Task<List<EnrolmentModel>> GetByStudentUserIdAsync(string studentUserId)
        {
            return await Collection
                .Find(e => e.StudentUserId == studentUserId)
                .SortBy(e => e.CreatedAt)
                .ToListAsync();
        }

        public Task<PagedResult<EnrolmentModel>> GetEnrolmentsAsync(EnrolmentFilter filter)
        {
            var filters = new List<FilterDefinition<EnrolmentModel>>
            {
                BuildSearchFilter(filter.SearchTerm, e => e.FirstName, e => e.LastName, e => e.Email, e => e.Mobile)
            };

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var statusStrings = filter.Statuses.Select(s => s.ToString()).ToList();
                filters.Add(Builders<EnrolmentModel>.Filter.In(e => e.Status, statusStrings));
            }

            if (!string.IsNullOrWhiteSpace(filter.OwnerUserId))
            {
                filters.Add(Builders<EnrolmentModel>.Filter.Eq(e => e.OwnerUserId, filter.OwnerUserId));
            }

            var mongoFilter = Builders<EnrolmentModel>.Filter.And(filters);
            var sort = Builders<EnrolmentModel>.Sort.Descending(e => e.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> ReplaceEnrolmentAsync(string id, EnrolmentModel enrolment)
        {
            return await ReplaceOneAsync(e => e.Id == id, enrolment);
        }

        public async Task<bool> DeleteEnrolmentAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(e => e.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
