using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the Tasks collection — see ShareService.Models.Task.TaskItemModel.
    // Indexes cover the two hot query paths: "who's involved" (My Tasks / department
    // scoped All Tasks, queried on assignerUserId/assigneeUserId + status) and "what's
    // linked to this record" (the Tasks tab on Enrolment/Enquiry/Application/Financial
    // Record detail pages).
    public class _044_AddTasksCollection_130826 : SafeMigrationBase
    {
        public override string MigrationId => "_044_AddTasksCollection_130826";
        public override string Name => "Add Tasks collection";
        public override string Description => "Creates the Tasks collection and indexes on assigneeUserId+status, assignerUserId+status, and the four linked-record id fields";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Tasks"))
            {
                await database.CreateCollectionAsync("Tasks");
                Console.WriteLine("✅ Created Tasks collection");
            }

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("assigneeUserId").Ascending("status"),
                    new CreateIndexOptions { Name = "idx_assigneeUserId_status" }));

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("assignerUserId").Ascending("status"),
                    new CreateIndexOptions { Name = "idx_assignerUserId_status" }));

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("enrolmentId"),
                    new CreateIndexOptions { Name = "idx_enrolmentId" }));

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("enquiryId"),
                    new CreateIndexOptions { Name = "idx_enquiryId" }));

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("applicationId"),
                    new CreateIndexOptions { Name = "idx_applicationId" }));

            await CreateIndexSafeAsync(database, "Tasks",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("financialRecordId"),
                    new CreateIndexOptions { Name = "idx_financialRecordId" }));

            Console.WriteLine("✅ Added Tasks indexes: assigneeUserId+status, assignerUserId+status, enrolmentId, enquiryId, applicationId, financialRecordId");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Tasks", "idx_assigneeUserId_status");
            await DropIndexSafeAsync(database, "Tasks", "idx_assignerUserId_status");
            await DropIndexSafeAsync(database, "Tasks", "idx_enrolmentId");
            await DropIndexSafeAsync(database, "Tasks", "idx_enquiryId");
            await DropIndexSafeAsync(database, "Tasks", "idx_applicationId");
            await DropIndexSafeAsync(database, "Tasks", "idx_financialRecordId");
            Console.WriteLine("✅ Rolled back Tasks indexes");
        }
    }
}
