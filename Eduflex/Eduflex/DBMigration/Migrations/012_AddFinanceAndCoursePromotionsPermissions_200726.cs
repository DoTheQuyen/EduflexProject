using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _012_AddFinanceAndCoursePromotionsPermissions_200726 : SafeMigrationBase
    {
        public override string MigrationId => "_012_AddFinanceAndCoursePromotionsPermissions_200726";
        public override string Name => "Add Finance/CoursePromotions Modules and Manager/Staff Roles";
        public override string Description => "Seeds Manager and Staff roles, Finance and CoursePromotions modules/permissions, and grants access per business rules";

        private static readonly string[] Actions = { "View", "Add", "Edit", "Delete" };

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

            // Step 1: seed Manager and Staff roles
            var managerRole = await GetOrCreateRole(rolesCollection, "Manager", "Manages finance and course promotions");
            var staffRole = await GetOrCreateRole(rolesCollection, "Staff", "Front-line staff with limited access");
            var adminRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Admin")).FirstOrDefaultAsync();

            if (adminRole == null)
            {
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping.");
                return;
            }

            // Step 2: seed Finance module + permissions
            var financePermissionIds = await SeedModuleAndPermissions(modulesCollection, permissionsCollection, "Finance", "Financial records and transactions");

            // Step 3: seed CoursePromotions module + permissions
            var coursePromoPermissionIds = await SeedModuleAndPermissions(modulesCollection, permissionsCollection, "CoursePromotions", "Promotional course listings");

            // Step 4: grant access per business rules
            // Admin: full access to both new modules (on top of whatever it already has)
            await GrantPermissions(rolesCollection, adminRole["_id"].AsObjectId, financePermissionIds.Values);
            await GrantPermissions(rolesCollection, adminRole["_id"].AsObjectId, coursePromoPermissionIds.Values);
            Console.WriteLine("✅ Admin granted full Finance + CoursePromotions access");

            // Manager: full access to both modules
            await GrantPermissions(rolesCollection, managerRole["_id"].AsObjectId, financePermissionIds.Values);
            await GrantPermissions(rolesCollection, managerRole["_id"].AsObjectId, coursePromoPermissionIds.Values);
            Console.WriteLine("✅ Manager granted full Finance + CoursePromotions access");

            // Staff: CoursePromotions view only, no Finance access
            await GrantPermissions(rolesCollection, staffRole["_id"].AsObjectId, new[] { coursePromoPermissionIds["View"] });
            Console.WriteLine("✅ Staff granted CoursePromotions view-only access");
        }

        private async Task<BsonDocument> GetOrCreateRole(IMongoCollection<BsonDocument> rolesCollection, string name, string description)
        {
            var role = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).FirstOrDefaultAsync();
            if (role == null)
            {
                role = new BsonDocument
                {
                    { "name", name },
                    { "description", description },
                    { "permissionIds", new BsonArray() }
                };
                await rolesCollection.InsertOneAsync(role);
                Console.WriteLine($"✅ Seeded {name} role");
            }
            return role;
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

            var financeAndCoursePromoPermissions = await permissionsCollection.Find(
                Builders<BsonDocument>.Filter.In("key", new[]
                {
                    "FinanceView", "FinanceAdd", "FinanceEdit", "FinanceDelete",
                    "CoursePromotionsView", "CoursePromotionsAdd", "CoursePromotionsEdit", "CoursePromotionsDelete"
                })).ToListAsync();

            var idsToRemove = financeAndCoursePromoPermissions.Select(p => p["_id"].AsObjectId.ToString()).ToList();

            var pullUpdate = Builders<BsonDocument>.Update.PullAll("permissionIds", idsToRemove);
            await rolesCollection.UpdateManyAsync(new BsonDocument(), pullUpdate);

            await rolesCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("name", "Manager"));
            await rolesCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("name", "Staff"));

            await permissionsCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id",
                financeAndCoursePromoPermissions.Select(p => p["_id"].AsObjectId)));

            await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("name", new[] { "Finance", "CoursePromotions" }));

            Console.WriteLine("✅ Rolled back Manager/Staff roles and Finance/CoursePromotions catalog entries");
        }
    }
}
