using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Adds the OpenRouter fallback endpoint URL to the existing Settings document — second
    // fallback provider, tried after Gemini and Groq are both unavailable.
    public class _048_SeedChatOpenRouterApiUrl_140826 : SafeMigrationBase
    {
        public override string MigrationId => "_048_SeedChatOpenRouterApiUrl_140826";
        public override string Name => "Seed chatOpenRouterApiUrl setting";
        public override string Description => "Adds chatOpenRouterApiUrl to the existing Settings document for the OpenRouter fallback provider";

        private const string ChatOpenRouterApiUrlDefault = "https://openrouter.ai/api/v1/chat/completions";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            if (existing == null)
            {
                Console.WriteLine("⚠️ No Settings document found — run migration 023 (AddSettingsCollection) first.");
                return;
            }

            if (existing.Contains("chatOpenRouterApiUrl"))
            {
                Console.WriteLine("✅ chatOpenRouterApiUrl already present on Settings document — skipping.");
                return;
            }

            var update = Builders<BsonDocument>.Update.Set("chatOpenRouterApiUrl", ChatOpenRouterApiUrlDefault);
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Seeded chatOpenRouterApiUrl on Settings document");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update.Unset("chatOpenRouterApiUrl");
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Removed chatOpenRouterApiUrl from Settings document");
        }
    }
}
