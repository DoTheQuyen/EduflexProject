using DBMigrationPostgres.Models;

namespace DBMigrationPostgres.Services.Interface;

public interface IMongoImportService
{
    Task<bool> IsMongoConfiguredAsync();

    // Copies Roles then Users from the source MongoDB into Postgres. Idempotent — rows whose
    // Id already exists in Postgres are skipped, so re-running only fills in what's missing.
    Task<bool> ImportRolesAndUsersAsync();

    // Read-only comparison of what's in Mongo vs what's in Postgres, for verifying an import
    // without changing anything.
    Task<List<ImportComparisonRow>> CompareAsync();
}
