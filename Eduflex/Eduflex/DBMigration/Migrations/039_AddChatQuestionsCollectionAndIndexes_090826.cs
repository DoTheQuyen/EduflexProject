using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _039_AddChatQuestionsCollectionAndIndexes_090826 : SafeMigrationBase
    {
        public override string MigrationId => "_039_AddChatQuestionsCollectionAndIndexes_090826";
        public override string Name => "Add ChatQuestions Collection and Indexes";
        public override string Description => "Creates the ChatQuestions collection with an index for cached-answer lookups, plus a TTL index so old logged questions auto-expire instead of growing the collection forever";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "ChatQuestions"))
            {
                await database.CreateCollectionAsync("ChatQuestions");
                Console.WriteLine("✅ Created ChatQuestions collection");
            }

            await CreateIndexSafeAsync(database, "ChatQuestions",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("normalizedQuestion").Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_normalizedQuestion_createdAt" }));
            Console.WriteLine("✅ Created lookup index on ChatQuestions.normalizedQuestion + createdAt");

            await CreateIndexSafeAsync(database, "ChatQuestions",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("createdAt"),
                    new CreateIndexOptions { Name = "idx_createdAt_ttl", ExpireAfter = TimeSpan.FromDays(30) }));
            Console.WriteLine("✅ Created TTL index on ChatQuestions.createdAt (auto-expires after 30 days)");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "ChatQuestions", "idx_normalizedQuestion_createdAt");
            await DropIndexSafeAsync(database, "ChatQuestions", "idx_createdAt_ttl");
        }
    }
}
