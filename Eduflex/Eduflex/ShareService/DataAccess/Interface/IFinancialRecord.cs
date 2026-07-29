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
        Task<bool> ReplaceAsync(string id, FinancialRecordModel record);
    }
}
