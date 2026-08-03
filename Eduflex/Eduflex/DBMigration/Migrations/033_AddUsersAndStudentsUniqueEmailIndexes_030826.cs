using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Replaces the old standalone "Create Database Tables" console step — Mongo creates
    // collections implicitly on first write/index, so the only thing that step actually did
    // that migrations didn't already cover was these two unique indexes.
    public class _033_AddUsersAndStudentsUniqueEmailIndexes_030826 : SafeMigrationBase
    {
        public override string MigrationId => "_033_AddUsersAndStudentsUniqueEmailIndexes_030826";
        public override string Name => "Add unique email indexes on Users and Students";
        public override string Description => "Creates unique indexes on Users.email and Students.email";

        public override async Task Up(IMongoDatabase database)
        {
            await CreateIndexSafeAsync(database, "Users",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("email"),
                    new CreateIndexOptions { Name = "idx_email_unique", Unique = true }));
            Console.WriteLine("✅ Created unique index on Users.email");

            await CreateIndexSafeAsync(database, "Students",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("email"),
                    new CreateIndexOptions { Name = "idx_email_unique", Unique = true }));
            Console.WriteLine("✅ Created unique index on Students.email");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Users", "idx_email_unique");
            await DropIndexSafeAsync(database, "Students", "idx_email_unique");
        }
    }
}
