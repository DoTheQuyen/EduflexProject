using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.StudentPaymentPlan;

namespace ShareService.DataAccess
{
    public class StudentPaymentPlanEntry : AuditableCollectionBase<StudentPaymentPlanEntryModel>, IStudentPaymentPlanEntry
    {
        public StudentPaymentPlanEntry(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<StudentPaymentPlanEntryModel>("StudentPaymentPlanEntries"), currentUser)
        {
        }

        public async Task<List<StudentPaymentPlanEntryModel>> GetByEnrolmentIdAsync(string enrolmentId)
        {
            return await Collection.Find(e => e.EnrolmentId == enrolmentId)
                .SortBy(e => e.InstalmentNumber)
                .ToListAsync();
        }

        public async Task<StudentPaymentPlanEntryModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<StudentPaymentPlanEntryModel>> GetDueByAsync(DateTime cutoffDate)
        {
            // Planned entries due within the window are "not yet invoiced, needs action";
            // Invoiced entries are included too so the caller can join against their
            // linked Invoice's live status and decide Overdue/Sent/Partial — Skipped
            // entries never surface in the queue.
            var filter = Builders<StudentPaymentPlanEntryModel>.Filter.And(
                Builders<StudentPaymentPlanEntryModel>.Filter.Lte(e => e.DueDate, cutoffDate),
                Builders<StudentPaymentPlanEntryModel>.Filter.Ne(e => e.Status, StudentPaymentPlanEntryStatuses.Skipped));

            return await Collection.Find(filter)
                .SortBy(e => e.DueDate)
                .ToListAsync();
        }

        public async Task<List<StudentPaymentPlanEntryModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<StudentPaymentPlanEntryModel>.Empty).ToListAsync();
        }

        public async Task<bool> CreateManyAsync(IEnumerable<StudentPaymentPlanEntryModel> entries)
        {
            foreach (var entry in entries)
            {
                await InsertOneAsync(entry);
            }
            return true;
        }

        public async Task<bool> CreateAsync(StudentPaymentPlanEntryModel entry)
        {
            await InsertOneAsync(entry);
            return true;
        }

        public async Task<bool> ReplaceAsync(string id, StudentPaymentPlanEntryModel entry)
        {
            return await ReplaceOneAsync(e => e.Id == id, entry);
        }
    }
}
