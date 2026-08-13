using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Tasks only has three permission keys (View/Add/Edit), not the usual four — there's
    // no TasksDelete (tasks are never deleted, only completed/reopened) and TasksViewAll
    // is seeded separately below since it's granted to a different, narrower set of
    // roles than the other three. See ShareService.Enums.Permissions.PermissionKey.
    public class _043_AddTasksModuleAndPermissions_130826 : SafeMigrationBase
    {
        public override string MigrationId => "_043_AddTasksModuleAndPermissions_130826";
        public override string Name => "Add Tasks Module Permissions";
        public override string Description => "Seeds the Tasks module/permissions: View/Add/Edit for Admin, Manager and Staff; ViewAll (department-scoped All Tasks page) for Admin and Manager only";

        private static readonly string[] Actions = { "View", "Add", "Edit" };

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

            var moduleId = await SeedModuleAsync(modulesCollection, "Tasks", "Assignable to-do items, optionally linked to an Enrolment/Enquiry/Application/Financial Record");

            var permissionIds = new Dictionary<string, ObjectId>();
            foreach (var action in Actions)
            {
                permissionIds[action] = await SeedPermissionAsync(permissionsCollection, moduleId, "Tasks", action);
            }
            var viewAllId = await SeedPermissionAsync(permissionsCollection, moduleId, "Tasks", "ViewAll");

            await GrantPermissions(rolesCollection, adminRole["_id"].AsObjectId, permissionIds.Values.Append(viewAllId));
            Console.WriteLine("✅ Admin granted full Tasks access (incl. ViewAll)");

            if (managerRole != null)
            {
                await GrantPermissions(rolesCollection, managerRole["_id"].AsObjectId, permissionIds.Values.Append(viewAllId));
                Console.WriteLine("✅ Manager granted full Tasks access (incl. ViewAll)");
            }

            if (staffRole != null)
            {
                await GrantPermissions(rolesCollection, staffRole["_id"].AsObjectId, permissionIds.Values);
                Console.WriteLine("✅ Staff granted View/Add/Edit Tasks access (My Tasks only — no ViewAll)");
            }
        }

        private async Task<ObjectId> SeedModuleAsync(IMongoCollection<BsonDocument> modulesCollection, string moduleName, string moduleDescription)
        {
            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", moduleName)).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument { { "name", moduleName }, { "description", moduleDescription } };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine($"✅ Seeded {moduleName} module");
            }
            return module["_id"].AsObjectId;
        }

        private async Task<ObjectId> SeedPermissionAsync(IMongoCollection<BsonDocument> permissionsCollection, ObjectId moduleId, string moduleName, string action)
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
            return permission["_id"].AsObjectId;
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

            var taskPermissions = await permissionsCollection.Find(
                Builders<BsonDocument>.Filter.In("key", new[] { "TasksView", "TasksAdd", "TasksEdit", "TasksViewAll" })).ToListAsync();

            var idsToRemove = taskPermissions.Select(p => p["_id"].AsObjectId.ToString()).ToList();

            var pullUpdate = Builders<BsonDocument>.Update.PullAll("permissionIds", idsToRemove);
            await rolesCollection.UpdateManyAsync(new BsonDocument(), pullUpdate);

            await permissionsCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id",
                taskPermissions.Select(p => p["_id"].AsObjectId)));

            await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "Tasks"));

            Console.WriteLine("✅ Rolled back Tasks catalog entries");
        }
    }
}
