using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _024_AddSettingsPermissions_280726 : SafeMigrationBase
    {
        public override string MigrationId => "_024_AddSettingsPermissions_280726";
        public override string Name => "Add Settings Module Permission";
        public override string Description => "Seeds the Settings module/permission (Edit only — reading effective settings just needs [Authorize], since students need it to validate application-form uploads). Admin gets edit access; Manager/Staff do not, since this covers app-wide configuration.";

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
            if (adminRole == null)
            {
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping.");
                return;
            }

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Settings")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "Settings" },
                    { "description", "App-wide configuration (feedback/course-promotion display counts, application-form document upload limits)" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded Settings module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "SettingsEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                permission = new BsonDocument
                {
                    { "moduleId", moduleId.ToString() },
                    { "action", "Edit" },
                    { "key", "SettingsEdit" },
                    { "description", "Edit Settings" }
                };
                await permissionsCollection.InsertOneAsync(permission);
                Console.WriteLine("✅ Seeded permission: SettingsEdit");
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Admin granted Settings edit access");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var modulesCollection = database.GetCollection<BsonDocument>("Modules");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "SettingsEdit")).FirstOrDefaultAsync();
            if (permission != null)
            {
                var idString = permission["_id"].AsObjectId.ToString();
                await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Pull("permissionIds", idString));
                await permissionsCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"].AsObjectId));
            }

            await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "Settings"));
            Console.WriteLine("✅ Rolled back Settings catalog entries");
        }
    }
}
