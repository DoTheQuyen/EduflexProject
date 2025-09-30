using MongoDB.Driver;

namespace DBMigration.Services.Interface
{
    public interface IMigration
    {
        string MigrationId { get; }
        string Name { get; }
        string Description { get; }

        Task Up(IMongoDatabase database);
        Task Down(IMongoDatabase database); // Optional: for rollbacks
    }
}