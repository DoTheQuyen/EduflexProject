using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _010_AddRolesAndUserRoleId_200726 : SafeMigrationBase
    {
        public override string MigrationId => "_010_AddRolesAndUserRoleId_200726";
        public override string Name => "Add Roles Collection and Map Users to Roles";
        public override string Description => "Seeds Admin/Student roles, adds roleId to every user, removes the old free-text role field";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
            {
                Console.WriteLine("⚠️ Users collection doesn't exist. Skipping migration.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");

            var adminRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Admin")).FirstOrDefaultAsync();
            if (adminRole == null)
            {
                adminRole = new BsonDocument
                {
                    { "name", "Admin" },
                    { "description", "Full administrative access" },
                    { "permissions", new BsonArray { "applications.manage" } }
                };
                await rolesCollection.InsertOneAsync(adminRole);
                Console.WriteLine("✅ Seeded Admin role");
            }
            else
            {
                Console.WriteLine("ℹ️ Admin role already exists");
            }

            var studentRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Student")).FirstOrDefaultAsync();
            if (studentRole == null)
            {
                studentRole = new BsonDocument
                {
                    { "name", "Student" },
                    { "description", "Standard authenticated user" },
                    { "permissions", new BsonArray() }
                };
                await rolesCollection.InsertOneAsync(studentRole);
                Console.WriteLine("✅ Seeded Student role");
            }
            else
            {
                Console.WriteLine("ℹ️ Student role already exists");
            }

            var adminRoleId = adminRole["_id"].AsObjectId;
            var studentRoleId = studentRole["_id"].AsObjectId;

            var usersCollection = database.GetCollection<BsonDocument>("Users");
            var users = await usersCollection.Find(new BsonDocument()).ToListAsync();

            int migratedCount = 0;
            foreach (var user in users)
            {
                var oldRole = user.Contains("role") ? user["role"].AsString : "Student";
                var newRoleId = oldRole == "Admin" ? adminRoleId : studentRoleId;

                var update = Builders<BsonDocument>.Update
                    .Set("roleId", newRoleId.ToString())
                    .Unset("role");

                var result = await usersCollection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", user["_id"]),
                    update
                );

                if (result.ModifiedCount > 0)
                    migratedCount++;
            }

            Console.WriteLine($"✅ Migrated {migratedCount} users to roleId references");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Users"))
                return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var usersCollection = database.GetCollection<BsonDocument>("Users");

            var roles = await rolesCollection.Find(new BsonDocument()).ToListAsync();
            var users = await usersCollection.Find(Builders<BsonDocument>.Filter.Exists("roleId")).ToListAsync();

            int rolledBackCount = 0;
            foreach (var user in users)
            {
                var roleId = user["roleId"].AsString;
                var role = roles.FirstOrDefault(r => r["_id"].AsObjectId.ToString() == roleId);
                var roleName = role != null ? role["name"].AsString : "Student";

                var update = Builders<BsonDocument>.Update
                    .Set("role", roleName)
                    .Unset("roleId");

                var result = await usersCollection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", user["_id"]),
                    update
                );

                if (result.ModifiedCount > 0)
                    rolledBackCount++;
            }

            await database.DropCollectionAsync("Roles");
            Console.WriteLine($"✅ Rolled back {rolledBackCount} users and dropped Roles collection");
        }
    }
}