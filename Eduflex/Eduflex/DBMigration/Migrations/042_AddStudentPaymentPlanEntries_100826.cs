using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the StudentPaymentPlanEntries collection — the student-side counterpart
    // to the partner commission claim schedule (FinancialRecordModel.InvoicePlan), but
    // flat/top-level rather than embedded so the Action Queue and Accounts screens can
    // query across every student's instalments without scanning FinancialRecords. See
    // ShareService.Models.StudentPaymentPlan.StudentPaymentPlanEntryModel.
    public class _042_AddStudentPaymentPlanEntries_100826 : SafeMigrationBase
    {
        public override string MigrationId => "_042_AddStudentPaymentPlanEntries_100826";
        public override string Name => "Add StudentPaymentPlanEntries collection";
        public override string Description => "Creates the StudentPaymentPlanEntries collection and indexes on enrolmentId, dueDate, and status+dueDate";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "StudentPaymentPlanEntries"))
            {
                await database.CreateCollectionAsync("StudentPaymentPlanEntries");
                Console.WriteLine("✅ Created StudentPaymentPlanEntries collection");
            }

            await CreateIndexSafeAsync(database, "StudentPaymentPlanEntries",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("enrolmentId"),
                    new CreateIndexOptions { Name = "idx_enrolmentId" }));

            await CreateIndexSafeAsync(database, "StudentPaymentPlanEntries",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("status").Ascending("dueDate"),
                    new CreateIndexOptions { Name = "idx_status_dueDate" }));

            Console.WriteLine("✅ Added StudentPaymentPlanEntries indexes: enrolmentId, status+dueDate");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "StudentPaymentPlanEntries", "idx_enrolmentId");
            await DropIndexSafeAsync(database, "StudentPaymentPlanEntries", "idx_status_dueDate");
            Console.WriteLine("✅ Rolled back StudentPaymentPlanEntries indexes");
        }
    }
}
