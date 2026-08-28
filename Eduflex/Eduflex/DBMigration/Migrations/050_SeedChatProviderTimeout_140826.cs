using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Moves the per-provider HTTP timeout for the Gemini/Groq/OpenRouter fallback chain off a
    // hardcoded constant in ChatService and onto the Settings document, so it can be tuned
    // without a redeploy.
    public class _050_SeedChatProviderTimeout_140826 : SafeMigrationBase
    {
        public override string MigrationId => "_050_SeedChatProviderTimeout_140826";
        public override string Name => "Seed chatProviderTimeoutSeconds setting";
        public override string Description => "Adds chatProviderTimeoutSeconds to the existing Settings document";

        private const int ChatProviderTimeoutSecondsDefault = 12;

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            if (existing == null)
            {
                Console.WriteLine("⚠️ No Settings document found — run migration 023 (AddSettingsCollection) first.");
                return;
            }

            if (existing.Contains("chatProviderTimeoutSeconds"))
            {
                Console.WriteLine("✅ chatProviderTimeoutSeconds already present on Settings document — skipping.");
                return;
            }

            var update = Builders<BsonDocument>.Update.Set("chatProviderTimeoutSeconds", ChatProviderTimeoutSecondsDefault);
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Seeded chatProviderTimeoutSeconds on Settings document");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update.Unset("chatProviderTimeoutSeconds");
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Removed chatProviderTimeoutSeconds from Settings document");
        }
    }
}
