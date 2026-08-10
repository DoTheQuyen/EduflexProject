using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Migration 034 indexed invoiceNo (unique) and relatedEnrolmentId, but missed two
    // query patterns that were already live: InvoicesController.GetByFinancialRecord
    // filters on relatedFinancialRecordId with no index at all, and GetAll filters on
    // category+status together (see InvoiceService.GetAllAsync) with only single-field
    // indexes to fall back on.
    public class _041_AddInvoicesFinancialRecordAndStatusIndexes_100826 : SafeMigrationBase
    {
        public override string MigrationId => "_041_AddInvoicesFinancialRecordAndStatusIndexes_100826";
        public override string Name => "Add missing Invoices indexes";
        public override string Description => "Adds an index on relatedFinancialRecordId and a compound index on category+status to the Invoices collection";

        public override async Task Up(IMongoDatabase database)
        {
            await CreateIndexSafeAsync(database, "Invoices",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("relatedFinancialRecordId"),
                    new CreateIndexOptions { Name = "idx_relatedFinancialRecordId" }));

            await CreateIndexSafeAsync(database, "Invoices",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("category").Ascending("status"),
                    new CreateIndexOptions { Name = "idx_category_status" }));

            Console.WriteLine("✅ Added Invoices indexes: relatedFinancialRecordId, category+status");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Invoices", "idx_relatedFinancialRecordId");
            await DropIndexSafeAsync(database, "Invoices", "idx_category_status");
            Console.WriteLine("✅ Rolled back Invoices index additions");
        }
    }
}
