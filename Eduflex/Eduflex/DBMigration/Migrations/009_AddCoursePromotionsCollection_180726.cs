using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _009_AddCoursePromotionsCollection_180726 : SafeMigrationBase
    {
        public override string MigrationId => "_009_AddCoursePromotionsCollection_180726";
        public override string Name => "Add CoursePromotions Collection";
        public override string Description => "Create the CoursePromotions collection with indexes for the public offers carousel and the staff management list";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "CoursePromotions"))
            {
                await database.CreateCollectionAsync("CoursePromotions");
                Console.WriteLine("✅ Created CoursePromotions collection");
            }

            await CreateIndexSafeAsync(database, "CoursePromotions",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("displayOrder").Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_displayOrder_createdAt" }));

            await CreateIndexSafeAsync(database, "CoursePromotions",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("isFeatured").Ascending("expiryDate"),
                    new CreateIndexOptions { Name = "idx_isFeatured_expiryDate" }));

            var existingIndexes = await GetExistingIndexNamesAsync(database, "CoursePromotions");
            Console.WriteLine($"📊 Current indexes in CoursePromotions: {string.Join(", ", existingIndexes)}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "CoursePromotions", "idx_displayOrder_createdAt");
            await DropIndexSafeAsync(database, "CoursePromotions", "idx_isFeatured_expiryDate");
        }
    }
}
