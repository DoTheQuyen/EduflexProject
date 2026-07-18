using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _007_AddEnquiriesCollection_170726 : SafeMigrationBase
    {
        public override string MigrationId => "_007_AddEnquiriesCollection_170726";
        public override string Name => "Add Enquiries Collection";
        public override string Description => "Create the Enquiries collection with indexes for email lookup and status/date filtering";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Enquiries"))
            {
                await database.CreateCollectionAsync("Enquiries");
                Console.WriteLine("✅ Created Enquiries collection");
            }

            await CreateIndexSafeAsync(database, "Enquiries",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("email"),
                    new CreateIndexOptions { Name = "idx_email" }));

            var statusDateKeys = Builders<BsonDocument>.IndexKeys
                .Ascending("status")
                .Descending("createdAt");

            await CreateIndexSafeAsync(database, "Enquiries",
                new CreateIndexModel<BsonDocument>(statusDateKeys, new CreateIndexOptions { Name = "idx_status_createdAt" }));

            var existingIndexes = await GetExistingIndexNamesAsync(database, "Enquiries");
            Console.WriteLine($"📊 Current indexes in Enquiries: {string.Join(", ", existingIndexes)}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Enquiries", "idx_email");
            await DropIndexSafeAsync(database, "Enquiries", "idx_status_createdAt");
        }
    }
}