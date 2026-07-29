using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _018_AddEnrolmentsPermissions_270726 : SafeMigrationBase
    {
        public override string MigrationId => "_018_AddEnrolmentsPermissions_270726";
        public override string Name => "Add Enrolments Module Permissions";
        public override string Description => "Seeds the Enrolments module/permissions (View/Add/Edit/Delete/Reassign); Admin+Manager get full access, Staff get everything except Reassign";

        private static readonly string[] Actions = { "View", "Add", "Edit", "Delete", "Reassign" };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Run migrations 010/011 first. Skipping.");
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
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping.");
                return;
            }

            var enrolmentPermissionIds = await SeedModuleAndPermissions(modulesCollection, permissionsCollection, "Enrolments", "Staff-managed student enrolment records");

            await GrantPermissions(rolesCollection, adminRole["_id"].AsObjectId, enrolmentPermissionIds.Values);
            Console.WriteLine("✅ Admin granted full Enrolments access");

            if (managerRole != null)
            {
                await GrantPermissions(rolesCollection, managerRole["_id"].AsObjectId, enrolmentPermissionIds.Values);
                Console.WriteLine("✅ Manager granted full Enrolments access (including Reassign)");
            }

            if (staffRole != null)
            {
                var staffPermissions = enrolmentPermissionIds
                    .Where(kv => kv.Key != "Reassign")
                    .Select(kv => kv.Value);
                await GrantPermissions(rolesCollection, staffRole["_id"].AsObjectId, staffPermissions);
                Console.WriteLine("✅ Staff granted Enrolments access (View/Add/Edit/Delete — Reassign is Manager-only)");
            }
        }

        private async Task<Dictionary<string, ObjectId>> SeedModuleAndPermissions(
            IMongoCollection<BsonDocument> modulesCollection,
            IMongoCollection<BsonDocument> permissionsCollection,
            string moduleName,
            string moduleDescription)
        {
            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", moduleName)).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", moduleName },
                    { "description", moduleDescription }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine($"✅ Seeded {moduleName} module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permissionIds = new Dictionary<string, ObjectId>();

            foreach (var action in Actions)
            {
                var key = $"{moduleName}{action}";
                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", key)).FirstOrDefaultAsync();
                if (permission == null)
                {
                    permission = new BsonDocument
                    {
                        { "moduleId", moduleId.ToString() },
                        { "action", action },
                        { "key", key },
                        { "description", $"{action} {moduleName}" }
                    };
                    await permissionsCollection.InsertOneAsync(permission);
                    Console.WriteLine($"✅ Seeded permission: {key}");
                }
                permissionIds[action] = permission["_id"].AsObjectId;
            }

            return permissionIds;
        }

        private async Task GrantPermissions(IMongoCollection<BsonDocument> rolesCollection, ObjectId roleId, IEnumerable<ObjectId> permissionIds)
        {
            var idStrings = permissionIds.Select(id => id.ToString());
            var update = Builders<BsonDocument>.Update.AddToSetEach("permissionIds", idStrings);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", roleId), update);
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
                return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var modulesCollection = database.GetCollection<BsonDocument>("Modules");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var enrolmentPermissions = await permissionsCollection.Find(
                Builders<BsonDocument>.Filter.In("key", new[]
                {
                    "EnrolmentsView", "EnrolmentsAdd", "EnrolmentsEdit", "EnrolmentsDelete", "EnrolmentsReassign"
                })).ToListAsync();

            var idsToRemove = enrolmentPermissions.Select(p => p["_id"].AsObjectId.ToString()).ToList();

            var pullUpdate = Builders<BsonDocument>.Update.PullAll("permissionIds", idsToRemove);
            await rolesCollection.UpdateManyAsync(new BsonDocument(), pullUpdate);

            await permissionsCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id",
                enrolmentPermissions.Select(p => p["_id"].AsObjectId)));

            await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "Enrolments"));

            Console.WriteLine("✅ Rolled back Enrolments catalog entries");
        }
    }
}
