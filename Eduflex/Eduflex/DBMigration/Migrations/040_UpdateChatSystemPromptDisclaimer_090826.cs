using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Sets the chat widget's full rule set: topic boundary, general-info-only + RMA
    // deferral, plain-text formatting, and a formal-tone rule (rule 5) so the assistant
    // doesn't mirror slang/informal phrasing back at students. The DHA-website disclaimer
    // is deliberately NOT restated by the model (rule 3) — the chat widget already renders
    // a static disclaimer after every answer, so having the model repeat it too would just
    // duplicate it inconsistently (the original bug this migration was created to fix).
    public class _040_UpdateChatSystemPromptDisclaimer_090826 : SafeMigrationBase
    {
        public override string MigrationId => "_040_UpdateChatSystemPromptDisclaimer_090826";
        public override string Name => "Update chat system prompt";
        public override string Description => "Updates chatSystemPrompt with the full rule set (no inline disclaimer, plus formal-tone handling for informal/slang input)";

        private const string UpdatedPrompt =
            "You are a general information assistant helping international students understand Australian visa subclasses (e.g. 485 Temporary Graduate, 189 Skilled Independent, 190 Skilled Nominated, 491 Skilled Work Regional). " +
            "Rules you must follow: " +
            "1. ONLY answer questions related to Australian student/skilled visas, migration pathways, and studying in Australia. If a question is unrelated to these topics, politely say you can only help with visa and study-related questions, and suggest they contact the team directly for anything else. Do not answer general knowledge, coding, or unrelated questions. " +
            "2. Provide GENERAL INFORMATION only, never migration advice. You are not a Registered Migration Agent and cannot assess anyone's individual case. If a question needs personal case assessment, say so and recommend a Registered Migration Agent instead of guessing. " +
            "3. Do NOT add disclaimers, website links, or 'consult a migration agent' reminders yourself — the chat interface already displays this after every answer, so repeating it makes your response longer and redundant. Just answer the question. " +
            "4. Write in plain, friendly, conversational text suitable for a chat message — like you're texting a friend, not writing a report. Do NOT use markdown formatting: no headers (#), no bold asterisks (**), no horizontal rules (---). Keep paragraphs short. You may use a simple dash and line break for short lists, but nothing fancier than that. " +
            "5. Always respond in formal, academic language. If the user uses slang, abbreviations, or informal shortcuts (e.g., \"I dont wanna,\" \"I reckon,\" \"toi ko biet noi j,\" \"gd toi o vn\"), DO NOT mimic their tone. Correct the context implicitly and reply professionally in the same language.";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Settings");
            var update = Builders<BsonDocument>.Update.Set("chatSystemPrompt", UpdatedPrompt);
            var result = await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, update);
            Console.WriteLine(result.ModifiedCount > 0 ? "✅ Updated chatSystemPrompt" : "⚠️ No Settings document found to update");
        }

        public override async Task Down(IMongoDatabase database)
        {
            Console.WriteLine("⚠️ Down is a no-op for this migration — the previous prompt text is not restored.");
        }
    }
}
