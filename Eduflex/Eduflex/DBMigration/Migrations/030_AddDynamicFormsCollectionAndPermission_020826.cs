using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the DynamicFormTemplates collection, seeds the single DynamicFormsEdit
    // permission (Admin-only — see docs/08-dynamic-form-module-design.md §5 for why this
    // is one flat key rather than a View/Add/Edit/Delete set), and seeds the two system
    // email templates the Dynamic Forms request/withdraw flow sends (reusing the existing
    // generic EmailTemplates infrastructure, same as migration 027's invoice template).
    public class _030_AddDynamicFormsCollectionAndPermission_020826 : SafeMigrationBase
    {
        public override string MigrationId => "_030_AddDynamicFormsCollectionAndPermission_020826";
        public override string Name => "Add Dynamic Forms module";
        public override string Description => "Creates DynamicFormTemplates collection + indexes, seeds DynamicFormsEdit permission (Admin only) and the request/withdraw system email templates";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "DynamicFormTemplates"))
            {
                await database.CreateCollectionAsync("DynamicFormTemplates");
                Console.WriteLine("✅ Created DynamicFormTemplates collection");
            }

            await CreateIndexSafeAsync(database, "DynamicFormTemplates",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("status"),
                    new CreateIndexOptions { Name = "idx_status" }));

            await CreateIndexSafeAsync(database, "DynamicFormTemplates",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("boundStepKey"),
                    new CreateIndexOptions { Name = "idx_boundStepKey" }));

            await SeedPermissionAsync(database);
            await SeedEmailTemplatesAsync(database);
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

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "DynamicForms")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "DynamicForms" },
                    { "description", "Dynamic form templates requestable from students during enrolment" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded DynamicForms module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "DynamicFormsEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                permission = new BsonDocument
                {
                    { "moduleId", moduleId.ToString() },
                    { "action", "Edit" },
                    { "key", "DynamicFormsEdit" },
                    { "description", "Edit Dynamic Forms" }
                };
                await permissionsCollection.InsertOneAsync(permission);
                Console.WriteLine("✅ Seeded permission: DynamicFormsEdit");
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Admin granted Dynamic Forms edit access");
        }

        private async Task SeedEmailTemplatesAsync(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "EmailTemplates"))
            {
                await database.CreateCollectionAsync("EmailTemplates");
            }

            var templatesCollection = database.GetCollection<BsonDocument>("EmailTemplates");
            var now = DateTime.UtcNow;

            var defaults = new (string Key, string Name, string Subject, string Body)[]
            {
                ("dynamic-form-request", "Dynamic form requested",
                    "Please complete: {{formName}}",
                    "Hi {{studentFirstName}},\n\n{{staffName}} has requested that you complete a form: {{formName}}.\n\nPlease log in to your student portal to fill it out:\n{{portalLink}}\n\nKind regards,\n{{staffName}}"),
                ("dynamic-form-withdrawn", "Form request withdrawn",
                    "Request withdrawn: {{formName}}",
                    "Hi {{studentFirstName}},\n\n{{staffName}} has withdrawn the request for you to complete: {{formName}}. No action is needed from you.\n\nKind regards,\n{{staffName}}"),
            };

            foreach (var template in defaults)
            {
                var existing = await templatesCollection.Find(Builders<BsonDocument>.Filter.Eq("key", template.Key)).FirstOrDefaultAsync();
                if (existing == null)
                {
                    await templatesCollection.InsertOneAsync(new BsonDocument
                    {
                        { "key", template.Key },
                        { "name", template.Name },
                        { "subject", template.Subject },
                        { "body", template.Body },
                        { "isSystemDefault", true },
                        { "createdAt", now },
                        { "updatedAt", now }
                    });
                    Console.WriteLine($"✅ Seeded email template: {template.Key}");
                }
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "DynamicFormTemplates", "idx_status");
            await DropIndexSafeAsync(database, "DynamicFormTemplates", "idx_boundStepKey");

            if (await CollectionExistsAsync(database, "Roles"))
            {
                var rolesCollection = database.GetCollection<BsonDocument>("Roles");
                var modulesCollection = database.GetCollection<BsonDocument>("Modules");
                var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "DynamicFormsEdit")).FirstOrDefaultAsync();
                if (permission != null)
                {
                    var idString = permission["_id"].AsObjectId.ToString();
                    await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Pull("permissionIds", idString));
                    await permissionsCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"].AsObjectId));
                }

                await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "DynamicForms"));
            }

            if (await CollectionExistsAsync(database, "EmailTemplates"))
            {
                var templatesCollection = database.GetCollection<BsonDocument>("EmailTemplates");
                await templatesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("key", new[] { "dynamic-form-request", "dynamic-form-withdrawn" }));
            }

            Console.WriteLine("✅ Rolled back Dynamic Forms module");
        }
    }
}
