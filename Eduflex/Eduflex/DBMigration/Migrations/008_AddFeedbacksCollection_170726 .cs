using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _008_AddFeedbacksCollection_170726 : SafeMigrationBase
    {
        public override string MigrationId => "_008_AddFeedbacksCollection_170726";
        public override string Name => "Add Feedbacks Collection";
        public override string Description => "Create the Feedbacks collection with an index for fetching the latest testimonials";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Feedbacks"))
            {
                await database.CreateCollectionAsync("Feedbacks");
                Console.WriteLine("✅ Created Feedbacks collection");
            }

            await CreateIndexSafeAsync(database, "Feedbacks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_createdAt" }));

            var existingIndexes = await GetExistingIndexNamesAsync(database, "Feedbacks");
            Console.WriteLine($"📊 Current indexes in Feedbacks: {string.Join(", ", existingIndexes)}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Feedbacks", "idx_createdAt");
        }
    }
}