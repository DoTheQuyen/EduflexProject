using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Moves the per-provider chat model IDs off appsettings.json/GeminiSettings.Model etc. and
    // onto the Settings document, so a model version can be pinned/swapped without a redeploy.
    public class _049_SeedChatModels_140826 : SafeMigrationBase
    {
        public override string MigrationId => "_049_SeedChatModels_140826";
        public override string Name => "Seed chat model settings";
        public override string Description => "Adds chatGeminiModel, chatGroqModel and chatOpenRouterModel to the existing Settings document";

        private const string ChatGeminiModelDefault = "gemini-3.6-flash";
        private const string ChatGroqModelDefault = "llama-3.3-70b-versatile";
        private const string ChatOpenRouterModelDefault = "nvidia/nemotron-3.5-lightning:free";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            if (existing == null)
            {
                Console.WriteLine("⚠️ No Settings document found — run migration 023 (AddSettingsCollection) first.");
                return;
            }

            if (existing.Contains("chatGeminiModel"))
            {
                Console.WriteLine("✅ chat model fields already present on Settings document — skipping.");
                return;
            }

            var update = Builders<BsonDocument>.Update
                .Set("chatGeminiModel", ChatGeminiModelDefault)
                .Set("chatGroqModel", ChatGroqModelDefault)
                .Set("chatOpenRouterModel", ChatOpenRouterModelDefault);
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Seeded chatGeminiModel, chatGroqModel and chatOpenRouterModel on Settings document");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update
                .Unset("chatGeminiModel")
                .Unset("chatGroqModel")
                .Unset("chatOpenRouterModel");
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Removed chat model fields from Settings document");
        }
    }
}
