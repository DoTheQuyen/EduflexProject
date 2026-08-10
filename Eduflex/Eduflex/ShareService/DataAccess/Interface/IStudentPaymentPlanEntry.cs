using ShareService.Models.StudentPaymentPlan;

namespace ShareService.DataAccess.Interface
{
    public interface IStudentPaymentPlanEntry
    {
        Task<List<StudentPaymentPlanEntryModel>> GetByEnrolmentIdAsync(string enrolmentId);
        Task<StudentPaymentPlanEntryModel?> GetByIdAsync(string id);

        // Everything due or overdue by cutoffDate and not yet fully resolved — the Action
        // Queue's source query. Bounded by an index on dueDate+status, not a full scan.
        Task<List<StudentPaymentPlanEntryModel>> GetDueByAsync(DateTime cutoffDate);

        // Whole-collection read for the Accounts portfolio screen's per-student rollup
        // (contract total, received, next due). A full scan is fine at this project's
        // scale; past a few tens of thousands of rows this should become a Mongo
        // aggregation pipeline instead of an in-memory GroupBy.
        Task<List<StudentPaymentPlanEntryModel>> GetAllAsync();

        Task<bool> CreateManyAsync(IEnumerable<StudentPaymentPlanEntryModel> entries);
        Task<bool> CreateAsync(StudentPaymentPlanEntryModel entry);
        Task<bool> ReplaceAsync(string id, StudentPaymentPlanEntryModel entry);
    }
}
