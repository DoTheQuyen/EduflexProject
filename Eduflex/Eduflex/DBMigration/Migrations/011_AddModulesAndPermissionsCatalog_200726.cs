using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _011_AddModulesAndPermissionsCatalog_200726 : SafeMigrationBase
    {
        public override string MigrationId => "_011_AddModulesAndPermissionsCatalog_200726";
        public override string Name => "Add Modules and Permissions Catalog";
        public override string Description => "Seeds the Modules/Permissions catalog and switches Roles from free-text permission strings to permissionIds references";

        private static readonly string[] Actions = { "View", "Add", "Edit", "Delete" };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Run migration 010 first. Skipping.");
                return;
            }

            var modulesCollection = database.GetCollection<BsonDocument>("Modules");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");
            var rolesCollection = database.GetCollection<BsonDocument>("Roles");

            // Step 1: seed the Applications module
            var applicationsModule = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Applications")).FirstOrDefaultAsync();
            if (applicationsModule == null)
            {
                applicationsModule = new BsonDocument
                {
                    { "name", "Applications" },
                    { "description", "Student application management" }
                };
                await modulesCollection.InsertOneAsync(applicationsModule);
                Console.WriteLine("✅ Seeded Applications module");
            }
            var applicationsModuleId = applicationsModule["_id"].AsObjectId;

            // Step 2: seed View/Add/Edit/Delete permissions for that module
            var permissionIds = new List<ObjectId>();
            foreach (var action in Actions)
            {
                var key = $"Applications{action}";
                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", key)).FirstOrDefaultAsync();
                if (permission == null)
                {
                    permission = new BsonDocument
                    {
                        { "moduleId", applicationsModuleId.ToString() },
                        { "action", action },
                        { "key", key },
                        { "description", $"{action} applications" }
                    };
                    await permissionsCollection.InsertOneAsync(permission);
                    Console.WriteLine($"✅ Seeded permission: {key}");
                }
                permissionIds.Add(permission["_id"].AsObjectId);
            }

            // Step 3: point Roles at permissionIds instead of free-text permission strings
            var adminRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Admin")).FirstOrDefaultAsync();
            if (adminRole != null)
            {
                var update = Builders<BsonDocument>.Update
                    .Set("permissionIds", permissionIds.Select(id => id.ToString()).ToList())
                    .Unset("permissions");

                await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"]), update);
                Console.WriteLine("✅ Admin role granted all Applications permissions");
            }

            var studentRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Student")).FirstOrDefaultAsync();
            if (studentRole != null)
            {
                var update = Builders<BsonDocument>.Update
                    .Set("permissionIds", new List<string>())
                    .Unset("permissions");

                await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", studentRole["_id"]), update);
                Console.WriteLine("✅ Student role migrated to permissionIds (empty)");
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
                return;

            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");
            var rolesCollection = database.GetCollection<BsonDocument>("Roles");

            var permissions = await permissionsCollection.Find(new BsonDocument()).ToListAsync();
            var roles = await rolesCollection.Find(Builders<BsonDocument>.Filter.Exists("permissionIds")).ToListAsync();

            foreach (var role in roles)
            {
                var ids = role["permissionIds"].AsBsonArray.Select(v => v.AsString).ToList();
                var keys = permissions
                    .Where(p => ids.Contains(p["_id"].AsObjectId.ToString()))
                    .Select(p => p["key"].AsString)
                    .ToList();

                var update = Builders<BsonDocument>.Update
                    .Set("permissions", keys)
                    .Unset("permissionIds");

                await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", role["_id"]), update);
            }

            await permissionsCollection.DeleteManyAsync(new BsonDocument());
            await database.DropCollectionAsync("Modules");
            Console.WriteLine("✅ Rolled back Roles to free-text permissions and cleared the catalog");
        }
    }
}
