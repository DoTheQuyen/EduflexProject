using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Backfills isActive=true on existing EmailTemplates documents (field didn't exist
    // before the Administration > Email Templates module), and seeds the single
    // EmailTemplatesEdit permission (Admin-only — same shape as migration 030's
    // DynamicFormsEdit; see PermissionKeyEnums.cs for why this is one flat key rather
    // than a View/Add/Edit/Delete set).
    public class _031_AddEmailTemplatesModuleAndPermission_020826 : SafeMigrationBase
    {
        public override string MigrationId => "_031_AddEmailTemplatesModuleAndPermission_020826";
        public override string Name => "Add Email Templates module";
        public override string Description => "Backfills isActive on EmailTemplates documents and seeds the EmailTemplatesEdit permission (Admin only)";

        public override async Task Up(IMongoDatabase database)
        {
            await BackfillIsActiveAsync(database);
            await SeedPermissionAsync(database);
        }

        private async Task BackfillIsActiveAsync(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "EmailTemplates"))
            {
                Console.WriteLine("⚠️ EmailTemplates collection doesn't exist. Run migration 017 first. Skipping backfill.");
                return;
            }

            var templatesCollection = database.GetCollection<BsonDocument>("EmailTemplates");
            var filter = Builders<BsonDocument>.Filter.Exists("isActive", false);
            var update = Builders<BsonDocument>.Update.Set("isActive", true);
            var result = await templatesCollection.UpdateManyAsync(filter, update);
            Console.WriteLine($"✅ Backfilled isActive=true on {result.ModifiedCount} email template(s)");
        }

        private async Task SeedPermissionAsync(IMongoDatabase database)
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
            if (adminRole == null)
            {
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping permission seed.");
                return;
            }

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "EmailTemplates")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "EmailTemplates" },
                    { "description", "Reusable email templates used across Enrolments, Finance and system notifications" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded EmailTemplates module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "EmailTemplatesEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                permission = new BsonDocument
                {
                    { "moduleId", moduleId.ToString() },
                    { "action", "Edit" },
                    { "key", "EmailTemplatesEdit" },
                    { "description", "Edit Email Templates" }
                };
                await permissionsCollection.InsertOneAsync(permission);
                Console.WriteLine("✅ Seeded permission: EmailTemplatesEdit");
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Admin granted Email Templates edit access");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (await CollectionExistsAsync(database, "EmailTemplates"))
            {
                var templatesCollection = database.GetCollection<BsonDocument>("EmailTemplates");
                await templatesCollection.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Empty,
                    Builders<BsonDocument>.Update.Unset("isActive"));
            }

            if (await CollectionExistsAsync(database, "Roles"))
            {
                var rolesCollection = database.GetCollection<BsonDocument>("Roles");
                var modulesCollection = database.GetCollection<BsonDocument>("Modules");
                var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "EmailTemplatesEdit")).FirstOrDefaultAsync();
                if (permission != null)
                {
                    var idString = permission["_id"].AsObjectId.ToString();
                    await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Pull("permissionIds", idString));
                    await permissionsCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"].AsObjectId));
                }

                await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "EmailTemplates"));
            }

            Console.WriteLine("✅ Rolled back Email Templates module");
        }
    }
}
