using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _019_BackfillUserMobileMandatory_270726 : SafeMigrationBase
    {
        public override string MigrationId => "_019_BackfillUserMobileMandatory_270726";
        public override string Name => "Backfill User.Mobile";
        public override string Description => "Users.mobile is now a mandatory field going forward. Backfills any existing user documents missing it with a placeholder so they still deserialize, and flags them for staff follow-up";

        private const string Placeholder = "UPDATE REQUIRED";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
            {
                Console.WriteLine("⚠️ Users collection doesn't exist. Skipping.");
                return;
            }

            var usersCollection = database.GetCollection<BsonDocument>("Users");

            var missingMobileFilter = Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Exists("mobile", false),
                Builders<BsonDocument>.Filter.Eq("mobile", BsonNull.Value),
                Builders<BsonDocument>.Filter.Eq("mobile", ""));

            var affectedCount = await usersCollection.CountDocumentsAsync(missingMobileFilter);
            if (affectedCount == 0)
            {
                Console.WriteLine("✅ All users already have a mobile number — nothing to backfill");
                return;
            }

            var update = Builders<BsonDocument>.Update.Set("mobile", Placeholder);
            var result = await usersCollection.UpdateManyAsync(missingMobileFilter, update);

            Console.WriteLine($"⚠️ Backfilled {result.ModifiedCount} user(s) with placeholder mobile \"{Placeholder}\" — ask each affected user to update their profile with a real mobile number.");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
                return;

            var usersCollection = database.GetCollection<BsonDocument>("Users");
            var placeholderFilter = Builders<BsonDocument>.Filter.Eq("mobile", Placeholder);
            var result = await usersCollection.UpdateManyAsync(placeholderFilter, Builders<BsonDocument>.Update.Unset("mobile"));
            Console.WriteLine($"✅ Reverted {result.ModifiedCount} placeholder mobile value(s)");
        }
    }
}
