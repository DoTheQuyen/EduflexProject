using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _002_AddUserLastLoginField_290925 : SafeMigrationBase
    {
        public override string MigrationId => "_002_AddUserLastLoginField_290925";
        public override string Name => "Add LastLogin Field to Users";
        public override string Description => "Add lastLogin field to track user login activity";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
            {
                Console.WriteLine("⚠️ Users collection doesn't exist. Skipping migration.");
                return;
            }

            // Check if field already exists
            if (!await FieldExistsAsync(database, "Users", "lastLogin"))
            {
                var collection = database.GetCollection<BsonDocument>("Users");

                // Add lastLogin field with null value to all existing users
                var update = Builders<BsonDocument>.Update.Set("lastLogin", BsonNull.Value);
                var result = await collection.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Exists("lastLogin", false),
                    update
                );

                Console.WriteLine($"✅ Added lastLogin field to {result.ModifiedCount} users");
            }
            else
            {
                Console.WriteLine("ℹ️ lastLogin field already exists in Users collection");
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
                return;

            // Remove lastLogin field from all users
            var collection = database.GetCollection<BsonDocument>("Users");
            var update = Builders<BsonDocument>.Update.Unset("lastLogin");
            var result = await collection.UpdateManyAsync(
                Builders<BsonDocument>.Filter.Exists("lastLogin", true),
                update
            );

            Console.WriteLine($"✅ Removed lastLogin field from {result.ModifiedCount} users");
        }
    }
}