using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Migration 034 seeded InvoiceTemplatesEdit and granted it to Admin only. Manager
    // already has full Finance access (FinanceView/Add/Edit/Delete, migration 012) and
    // owns the rest of the invoicing workflow via FinanceEdit (see InvoiceService.
    // RequireInvoiceActionPermissionAsync) — leaving template management and the sent-
    // invoice ledger Admin-only was an oversight, not a deliberate policy split.
    public class _040_ExtendInvoiceTemplatesEditToManager_100826 : SafeMigrationBase
    {
        public override string MigrationId => "_040_ExtendInvoiceTemplatesEditToManager_100826";
        public override string Name => "Extend InvoiceTemplatesEdit to Manager";
        public override string Description => "Grants the InvoiceTemplatesEdit permission to the Manager role, matching Manager's existing full access to the rest of the Finance module";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Skipping.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var managerRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Manager")).FirstOrDefaultAsync();
            if (managerRole == null)
            {
                Console.WriteLine("⚠️ Manager role not found. Skipping.");
                return;
            }

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "InvoiceTemplatesEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                Console.WriteLine("⚠️ InvoiceTemplatesEdit permission not found. Run migration 034 first. Skipping.");
                return;
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", managerRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Manager granted Invoice Templates edit access");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
                return;

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var managerRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Manager")).FirstOrDefaultAsync();
            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "InvoiceTemplatesEdit")).FirstOrDefaultAsync();

            if (managerRole != null && permission != null)
            {
                var idString = permission["_id"].AsObjectId.ToString();
                await rolesCollection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", managerRole["_id"].AsObjectId),
                    Builders<BsonDocument>.Update.Pull("permissionIds", idString));
            }

            Console.WriteLine("✅ Rolled back Manager's Invoice Templates edit access");
        }
    }
}
