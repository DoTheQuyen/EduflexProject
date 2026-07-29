using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _020_GrantStudentApplicationsPermissions_270726 : SafeMigrationBase
    {
        public override string MigrationId => "_020_GrantStudentApplicationsPermissions_270726";
        public override string Name => "Grant Applications permissions to Student, Staff and Manager roles";
        public override string Description => "Migration 011 seeded ApplicationsView/Add/Edit/Delete but only ever granted them to Admin, leaving Student's permissionIds empty and Staff/Manager with none at all (unlike every other module, which grants Staff/Manager access alongside Admin). Grants Student the View/Add they need for self-service (list/see their own applications, submit a new one), and completes the gap for Staff/Manager with full View/Add/Edit/Delete, matching the Enrolments pattern.";

        private static readonly string[] AllActions = { "View", "Add", "Edit", "Delete" };
        private static readonly string[] StudentActions = { "View", "Add" };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Run migration 010 first. Skipping.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var allKeys = AllActions.Select(a => $"Applications{a}").ToArray();
            var permissions = await permissionsCollection.Find(Builders<BsonDocument>.Filter.In("key", allKeys)).ToListAsync();

            if (permissions.Count != AllActions.Length)
            {
                Console.WriteLine("⚠️ ApplicationsView/Add/Edit/Delete permissions not found. Run migration 011 first. Skipping.");
                return;
            }

            var permissionIdByKey = permissions.ToDictionary(p => p["key"].AsString, p => p["_id"].AsObjectId.ToString());

            var studentRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Student")).FirstOrDefaultAsync();
            if (studentRole != null)
            {
                var studentPermissionIds = StudentActions.Select(a => permissionIdByKey[$"Applications{a}"]);
                await GrantPermissions(rolesCollection, studentRole["_id"].AsObjectId, studentPermissionIds);
                Console.WriteLine("✅ Student role granted ApplicationsView + ApplicationsAdd");
            }
            else
            {
                Console.WriteLine("⚠️ Student role not found. Skipping Student grant.");
            }

            var allPermissionIds = permissionIdByKey.Values;

            var managerRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Manager")).FirstOrDefaultAsync();
            if (managerRole != null)
            {
                await GrantPermissions(rolesCollection, managerRole["_id"].AsObjectId, allPermissionIds);
                Console.WriteLine("✅ Manager role granted full Applications access");
            }

            var staffRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Staff")).FirstOrDefaultAsync();
            if (staffRole != null)
            {
                await GrantPermissions(rolesCollection, staffRole["_id"].AsObjectId, allPermissionIds);
                Console.WriteLine("✅ Staff role granted full Applications access");
            }
        }

        private async Task GrantPermissions(IMongoCollection<BsonDocument> rolesCollection, ObjectId roleId, IEnumerable<string> permissionIds)
        {
            var update = Builders<BsonDocument>.Update.AddToSetEach("permissionIds", permissionIds);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", roleId), update);
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
                return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var allKeys = AllActions.Select(a => $"Applications{a}").ToArray();
            var permissions = await permissionsCollection.Find(Builders<BsonDocument>.Filter.In("key", allKeys)).ToListAsync();
            var idsToRemove = permissions.Select(p => p["_id"].AsObjectId.ToString()).ToList();

            var pullUpdate = Builders<BsonDocument>.Update.PullAll("permissionIds", idsToRemove);

            foreach (var roleName in new[] { "Student", "Staff", "Manager" })
            {
                var role = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", roleName)).FirstOrDefaultAsync();
                if (role == null) continue;
                await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", role["_id"]), pullUpdate);
            }

            Console.WriteLine("✅ Revoked Applications permissions granted by this migration from Student/Staff/Manager");
        }
    }
}
