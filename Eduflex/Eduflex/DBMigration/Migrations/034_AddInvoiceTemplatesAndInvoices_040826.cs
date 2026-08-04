using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the InvoiceTemplates (admin-configured branding/numbering) and Invoices
    // (sent-invoice ledger) collections, and seeds the InvoiceTemplatesEdit permission
    // (Admin-only, same shape as DynamicFormsEdit — see migration 030) onto the Admin role.
    public class _034_AddInvoiceTemplatesAndInvoices_040826 : SafeMigrationBase
    {
        public override string MigrationId => "_034_AddInvoiceTemplatesAndInvoices_040826";
        public override string Name => "Add Invoice Templates module";
        public override string Description => "Creates InvoiceTemplates + Invoices collections and indexes, seeds InvoiceTemplatesEdit permission (Admin only)";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "InvoiceTemplates"))
            {
                await database.CreateCollectionAsync("InvoiceTemplates");
                Console.WriteLine("✅ Created InvoiceTemplates collection");
            }

            await CreateIndexSafeAsync(database, "InvoiceTemplates",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("category"),
                    new CreateIndexOptions { Name = "idx_category" }));

            if (!await CollectionExistsAsync(database, "Invoices"))
            {
                await database.CreateCollectionAsync("Invoices");
                Console.WriteLine("✅ Created Invoices collection");
            }

            await CreateIndexSafeAsync(database, "Invoices",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("invoiceNo"),
                    new CreateIndexOptions { Name = "idx_invoiceNo", Unique = true }));

            await CreateIndexSafeAsync(database, "Invoices",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("relatedEnrolmentId"),
                    new CreateIndexOptions { Name = "idx_relatedEnrolmentId" }));

            await SeedPermissionAsync(database);
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

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "InvoiceTemplates")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "InvoiceTemplates" },
                    { "description", "Configurable invoice branding/numbering templates and the sent-invoice ledger" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded InvoiceTemplates module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "InvoiceTemplatesEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                permission = new BsonDocument
                {
                    { "moduleId", moduleId.ToString() },
                    { "action", "Edit" },
                    { "key", "InvoiceTemplatesEdit" },
                    { "description", "Edit Invoice Templates" }
                };
                await permissionsCollection.InsertOneAsync(permission);
                Console.WriteLine("✅ Seeded permission: InvoiceTemplatesEdit");
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Admin granted Invoice Templates edit access");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "InvoiceTemplates", "idx_category");
            await DropIndexSafeAsync(database, "Invoices", "idx_invoiceNo");
            await DropIndexSafeAsync(database, "Invoices", "idx_relatedEnrolmentId");

            if (await CollectionExistsAsync(database, "Roles"))
            {
                var rolesCollection = database.GetCollection<BsonDocument>("Roles");
                var modulesCollection = database.GetCollection<BsonDocument>("Modules");
                var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "InvoiceTemplatesEdit")).FirstOrDefaultAsync();
                if (permission != null)
                {
                    var idString = permission["_id"].AsObjectId.ToString();
                    await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Pull("permissionIds", idString));
                    await permissionsCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"].AsObjectId));
                }

                await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "InvoiceTemplates"));
            }

            Console.WriteLine("✅ Rolled back Invoice Templates module");
        }
    }
}
