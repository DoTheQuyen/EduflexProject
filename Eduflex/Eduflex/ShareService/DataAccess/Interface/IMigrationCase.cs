using ShareService.Common;
using ShareService.Models.MigrationCase;

namespace ShareService.DataAccess.Interface
{
    public interface IMigrationCase
    {
        Task<bool> CreateCaseAsync(MigrationCaseModel migrationCase);
        Task<MigrationCaseModel?> GetCaseAsync(string id);
        Task<PagedResult<MigrationCaseModel>> GetCasesAsync(MigrationCaseFilter filter);
        Task<long> CountAllAsync();
        Task<bool> ReplaceCaseAsync(string id, MigrationCaseModel migrationCase);
        Task<Dictionary<string, int>> GetMonthlyCountsAsync(DateTime since);
        Task<Dictionary<string, int>> GetStatusCountsAsync();
    }
}
