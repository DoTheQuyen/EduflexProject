using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _001_AddApplications_290925 : SafeMigrationBase
    {
        public override string MigrationId => "_001_AddApplications290925";
        public override string Name => "Add Applications Collection Indexes";
        public override string Description => "Create optimized indexes for Applications collection queries";

        public override async Task Up(IMongoDatabase database)
        {
            // Check if Applications collection exists
            if (!await CollectionExistsAsync(database, "Applications"))
            {
                // Create the collection if it doesn't exist
                await database.CreateCollectionAsync("Applications");
            }

            // Create compound index for student applications
            var studentIndexKeys = Builders<BsonDocument>.IndexKeys
                .Ascending("studentId")
                .Descending("dateApplied");

            await CreateIndexSafeAsync(database, "Applications",
                new CreateIndexModel<BsonDocument>(studentIndexKeys, new CreateIndexOptions { Name = "idx_student_date" }));

            // Create status index
            var statusIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("status");
            await CreateIndexSafeAsync(database, "Applications",
                new CreateIndexModel<BsonDocument>(statusIndexKeys, new CreateIndexOptions { Name = "idx_status" }));

            // Create compound index for status and date
            var statusDateIndexKeys = Builders<BsonDocument>.IndexKeys
                .Ascending("status")
                .Descending("dateApplied");

            await CreateIndexSafeAsync(database, "Applications",
                new CreateIndexModel<BsonDocument>(statusDateIndexKeys, new CreateIndexOptions { Name = "idx_status_date" }));

            // Log the indexes that were created
            var existingIndexes = await GetExistingIndexNamesAsync(database, "Applications");
            Console.WriteLine($"📊 Current indexes in Applications: {string.Join(", ", existingIndexes)}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            // Only drop indexes that we created
            await DropIndexSafeAsync(database, "Applications", "idx_student_date");
            await DropIndexSafeAsync(database, "Applications", "idx_status");
            await DropIndexSafeAsync(database, "Applications", "idx_status_date");
        }
    }
}