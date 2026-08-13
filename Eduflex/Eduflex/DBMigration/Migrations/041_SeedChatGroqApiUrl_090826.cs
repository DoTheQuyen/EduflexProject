using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Adds the Groq fallback endpoint URL to the existing Settings document — was
    // previously hardcoded in ChatService, now DB-configurable like chatApiUrl (Gemini).
    public class _041_SeedChatGroqApiUrl_090826 : SafeMigrationBase
    {
        public override string MigrationId => "_041_SeedChatGroqApiUrl_090826";
        public override string Name => "Seed chatGroqApiUrl setting";
        public override string Description => "Adds chatGroqApiUrl to the existing Settings document for the Groq fallback provider";

        private const string ChatGroqApiUrlDefault = "https://api.groq.com/openai/v1/chat/completions";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            if (existing == null)
            {
                Console.WriteLine("⚠️ No Settings document found — run migration 023 (AddSettingsCollection) first.");
                return;
            }

            if (existing.Contains("chatGroqApiUrl"))
            {
                Console.WriteLine("✅ chatGroqApiUrl already present on Settings document — skipping.");
                return;
            }

            var update = Builders<BsonDocument>.Update.Set("chatGroqApiUrl", ChatGroqApiUrlDefault);
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Seeded chatGroqApiUrl on Settings document");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update.Unset("chatGroqApiUrl");
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Removed chatGroqApiUrl from Settings document");
        }
    }
}
