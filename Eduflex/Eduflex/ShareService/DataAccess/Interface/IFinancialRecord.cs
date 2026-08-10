using ShareService.Common;
using ShareService.Models.Financial;

namespace ShareService.DataAccess.Interface
{
    public interface IFinancialRecord
    {
        Task<bool> CreateAsync(FinancialRecordModel record);
        Task<FinancialRecordModel?> GetByIdAsync(string id);
        Task<FinancialRecordModel?> GetByEnrolmentIdAsync(string enrolmentId);
        Task<PagedResult<FinancialRecordModel>> GetFinancialRecordsAsync(FinancialRecordFilter filter);

        // Whole-collection read for the Accounts/Action Queue screens, same trade-off as
        // IStudentPaymentPlanEntry.GetAllAsync — InvoicePlan is embedded per record, so
        // there's no way to query "claims due by X" across records without either this
        // or an aggregation pipeline with $unwind. Fine at this project's scale.
        Task<List<FinancialRecordModel>> GetAllAsync();

        Task<bool> ReplaceAsync(string id, FinancialRecordModel record);
    }
}
