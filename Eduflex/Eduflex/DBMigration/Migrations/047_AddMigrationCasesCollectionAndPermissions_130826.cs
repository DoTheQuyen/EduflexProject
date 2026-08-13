using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the MigrationCases collection (the case-execution side of the VISA Process
    // module — see ShareService/Models/MigrationCase/MigrationCaseModel.cs) and seeds its
    // permission set, mirroring migration 018's Enrolments pattern exactly:
    // Admin+Manager get full access, Staff get everything except Reassign.
    public class _047_AddMigrationCasesCollectionAndPermissions_130826 : SafeMigrationBase
    {
        public override string MigrationId => "_047_AddMigrationCasesCollectionAndPermissions_130826";
        public override string Name => "Add Migration Cases module";
        public override string Description => "Creates MigrationCases collection + indexes, seeds MigrationCases* permissions (View/Add/Edit/Delete/Reassign) — Admin+Manager full access, Staff everything except Reassign";

        private static readonly string[] Actions = { "View", "Add", "Edit", "Delete", "Reassign" };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "MigrationCases"))
            {
                await database.CreateCollectionAsync("MigrationCases");
                Console.WriteLine("✅ Created MigrationCases collection");
            }

            await CreateIndexSafeAsync(database, "MigrationCases",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("ownerUserId"),
                    new CreateIndexOptions { Name = "idx_ownerUserId" }));

            await CreateIndexSafeAsync(database, "MigrationCases",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("status"),
                    new CreateIndexOptions { Name = "idx_status" }));

            await CreateIndexSafeAsync(database, "MigrationCases",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("category"),
                    new CreateIndexOptions { Name = "idx_category" }));

            await CreateIndexSafeAsync(database, "MigrationCases",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("caseReference"),
                    new CreateIndexOptions { Name = "idx_caseReference", Unique = true }));

            await SeedPermissionsAsync(database);
        }

        private async Task SeedPermissionsAsync(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Run migrations 010/011 first. Skipping permission seed.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var modulesCollection = database.GetCollection<BsonDocument>("Modules");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var adminRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Admin")).FirstOrDefaultAsync();
            var managerRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Manager")).FirstOrDefaultAsync();
            var staffRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Staff")).FirstOrDefaultAsync();

            if (adminRole == null)
            {
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping permission seed.");
                return;
            }

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "MigrationCases")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "MigrationCases" },
                    { "description", "Generic, category-agnostic visa/migration cases started from a VISA Process Template" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded MigrationCases module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permissionIds = new Dictionary<string, ObjectId>();
            foreach (var action in Actions)
            {
                var key = $"MigrationCases{action}";
                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", key)).FirstOrDefaultAsync();
                if (permission == null)
                {
                    permission = new BsonDocument
                    {
                        { "moduleId", moduleId.ToString() },
                        { "action", action },
                        { "key", key },
                        { "description", $"{action} Migration Cases" }
                    };
                    await permissionsCollection.InsertOneAsync(permission);
                    Console.WriteLine($"✅ Seeded permission: {key}");
                }
                permissionIds[action] = permission["_id"].AsObjectId;
            }

            await GrantPermissionsAsync(rolesCollection, adminRole["_id"].AsObjectId, permissionIds.Values);
            Console.WriteLine("✅ Admin granted full Migration Cases access");

            if (managerRole != null)
            {
                await GrantPermissionsAsync(rolesCollection, managerRole["_id"].AsObjectId, permissionIds.Values);
                Console.WriteLine("✅ Manager granted full Migration Cases access (including Reassign)");
            }

            if (staffRole != null)
            {
                var staffPermissions = permissionIds.Where(kv => kv.Key != "Reassign").Select(kv => kv.Value);
                await GrantPermissionsAsync(rolesCollection, staffRole["_id"].AsObjectId, staffPermissions);
                Console.WriteLine("✅ Staff granted Migration Cases access (View/Add/Edit/Delete — Reassign is Manager-only)");
            }
        }

        private async Task GrantPermissionsAsync(IMongoCollection<BsonDocument> rolesCollection, ObjectId roleId, IEnumerable<ObjectId> permissionIds)
        {
            var idStrings = permissionIds.Select(id => id.ToString());
            var update = Builders<BsonDocument>.Update.AddToSetEach("permissionIds", idStrings);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", roleId), update);
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "MigrationCases", "idx_ownerUserId");
            await DropIndexSafeAsync(database, "MigrationCases", "idx_status");
            await DropIndexSafeAsync(database, "MigrationCases", "idx_category");
            await DropIndexSafeAsync(database, "MigrationCases", "idx_caseReference");

            if (await CollectionExistsAsync(database, "Roles"))
            {
                var rolesCollection = database.GetCollection<BsonDocument>("Roles");
                var modulesCollection = database.GetCollection<BsonDocument>("Modules");
                var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

                var casePermissions = await permissionsCollection.Find(
                    Builders<BsonDocument>.Filter.In("key", new[]
                    {
                        "MigrationCasesView", "MigrationCasesAdd", "MigrationCasesEdit", "MigrationCasesDelete", "MigrationCasesReassign"
                    })).ToListAsync();

                var idsToRemove = casePermissions.Select(p => p["_id"].AsObjectId.ToString()).ToList();
                await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.PullAll("permissionIds", idsToRemove));
                await permissionsCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", casePermissions.Select(p => p["_id"].AsObjectId)));
                await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "MigrationCases"));
            }

            Console.WriteLine("✅ Rolled back Migration Cases module");
        }
    }
}
