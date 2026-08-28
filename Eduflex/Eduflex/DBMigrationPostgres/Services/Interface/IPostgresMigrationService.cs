namespace DBMigrationPostgres.Services.Interface;

public interface IPostgresMigrationService
{
    Task<bool> RunMigrationsAsync();
    Task<List<string>> GetAppliedMigrationsAsync();
    Task<List<string>> GetPendingMigrationsAsync();
}
