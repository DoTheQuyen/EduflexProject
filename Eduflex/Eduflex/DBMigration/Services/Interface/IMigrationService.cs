using DBMigration.Models;
using MongoDB.Driver;

namespace DBMigration.Services.Interface
{
    public interface IMigrationService
    {
        Task<bool> RunMigrationsAsync();
        Task<List<MigrationRecord>> GetMigrationHistoryAsync();
        Task RollbackMigrationAsync(string migrationId);
    }
}