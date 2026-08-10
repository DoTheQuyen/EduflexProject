using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
   
    public class _038_SeedChatSettings_090826 : SafeMigrationBase
    {
        public override string MigrationId => "_038_SeedChatSettings_090826";
        public override string Name => "Seed chat widget settings";
        public override string Description => "Adds chatSystemPrompt and chatApiUrl fields to the existing Settings document for the Visa Q&A chat widget";

        private const string ChatSystemPromptDefault =
            "You are a general information assistant helping international students understand Australian visa subclasses (e.g. 485 Temporary Graduate, 189 Skilled Independent, 190 Skilled Nominated, 491 Skilled Work Regional). " +
            "Rules you must follow: " +
            "1. ONLY answer questions related to Australian student/skilled visas, migration pathways, and studying in Australia. If a question is unrelated to these topics, politely say you can only help with visa and study-related questions, and suggest they contact the team directly for anything else. Do not answer general knowledge, coding, or unrelated questions. " +
            "2. Provide GENERAL INFORMATION only, never migration advice. You are not a Registered Migration Agent and cannot assess anyone's individual case. " +
            "3. Always remind the student to confirm current details on the official Department of Home Affairs website (immi.homeaffairs.gov.au), since visa rules change. " +
            "4. If a question needs personal case assessment, say so and recommend a Registered Migration Agent instead of guessing. " +
            "5. Write in plain, friendly, conversational text suitable for a chat message — like you're texting a friend, not writing a report. Do NOT use markdown formatting: no headers (#), no bold asterisks (**), no horizontal rules (---). Keep paragraphs short. You may use a simple dash and line break for short lists, but nothing fancier than that.";

        private const string ChatApiUrlDefault = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            if (existing == null)
            {
                Console.WriteLine("⚠️ No Settings document found — run migration 023 (AddSettingsCollection) first.");
                return;
            }

            if (existing.Contains("chatSystemPrompt") && existing.Contains("chatApiUrl"))
            {
                Console.WriteLine("✅ Chat settings already present on Settings document — skipping.");
                return;
            }

            var update = Builders<BsonDocument>.Update
                .Set("chatSystemPrompt", ChatSystemPromptDefault)
                .Set("chatApiUrl", ChatApiUrlDefault);

            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Seeded chatSystemPrompt and chatApiUrl on Settings document");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update
                .Unset("chatSystemPrompt")
                .Unset("chatApiUrl");

            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine("✅ Removed chatSystemPrompt and chatApiUrl from Settings document");
        }
    }
}
