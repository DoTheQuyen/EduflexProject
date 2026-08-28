namespace DBMigrationPostgres.Services.Interface;

public interface IPostgresDatabaseService
{
    Task<bool> TestConnectionAsync();
    Task<List<string>> GetTableNamesAsync();
    Task<long> GetTableRowCountAsync(string tableName);
    Task DropAllTablesAsync();
    Task InsertTestDataAsync();
    Task ClearTestDataAsync();
}
