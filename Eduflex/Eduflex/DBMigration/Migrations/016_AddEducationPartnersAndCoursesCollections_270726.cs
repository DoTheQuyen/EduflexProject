using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _016_AddEducationPartnersAndCoursesCollections_270726 : SafeMigrationBase
    {
        public override string MigrationId => "_016_AddEducationPartnersAndCoursesCollections_270726";
        public override string Name => "Add EducationPartners and Courses Collections";
        public override string Description => "Create the EducationPartners and Courses collections with indexes for country grouping and the partner-to-course lookup";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "EducationPartners"))
            {
                await database.CreateCollectionAsync("EducationPartners");
                Console.WriteLine("✅ Created EducationPartners collection");
            }

            await CreateIndexSafeAsync(database, "EducationPartners",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("country"),
                    new CreateIndexOptions { Name = "idx_country" }));

            if (!await CollectionExistsAsync(database, "Courses"))
            {
                await database.CreateCollectionAsync("Courses");
                Console.WriteLine("✅ Created Courses collection");
            }

            await CreateIndexSafeAsync(database, "Courses",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("educationPartnerId"),
                    new CreateIndexOptions { Name = "idx_educationPartnerId" }));

            var partnerIndexes = await GetExistingIndexNamesAsync(database, "EducationPartners");
            Console.WriteLine($"📊 Current indexes in EducationPartners: {string.Join(", ", partnerIndexes)}");

            var courseIndexes = await GetExistingIndexNamesAsync(database, "Courses");
            Console.WriteLine($"📊 Current indexes in Courses: {string.Join(", ", courseIndexes)}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "EducationPartners", "idx_country");
            await DropIndexSafeAsync(database, "Courses", "idx_educationPartnerId");
        }
    }
}
