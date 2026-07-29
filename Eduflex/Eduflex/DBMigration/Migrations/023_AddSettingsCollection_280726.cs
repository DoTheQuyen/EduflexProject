using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _023_AddSettingsCollection_280726 : SafeMigrationBase
    {
        public override string MigrationId => "_023_AddSettingsCollection_280726";
        public override string Name => "Add Settings Collection";
        public override string Description => "Creates the singleton Settings collection and seeds one default document with the values previously hardcoded in appsettings.json (FeedbackSettings.DefaultLatestCount, CoursePromotionSettings.DefaultLatestCount) plus default document-upload limits (max size, allowed extensions, max file count) for the student application form.";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Settings"))
            {
                await database.CreateCollectionAsync("Settings");
                Console.WriteLine("✅ Created Settings collection");
            }

            var collection = database.GetCollection<BsonDocument>("Settings");
            var existing = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
            if (existing == null)
            {
                var now = DateTime.UtcNow;
                var defaultUploadLimit = new BsonDocument
                {
                    { "maxSizeMB", 5.0 },
                    { "allowedExtensions", new BsonArray { ".pdf", ".jpg", ".jpeg", ".png" } },
                    { "maxFileCount", 1 }
                };
                var otherUploadLimit = new BsonDocument
                {
                    { "maxSizeMB", 5.0 },
                    { "allowedExtensions", new BsonArray { ".pdf", ".jpg", ".jpeg", ".png" } },
                    { "maxFileCount", 4 }
                };

                var doc = new BsonDocument
                {
                    { "feedbackDefaultLatestCount", 10 },
                    { "coursePromotionDefaultLatestCount", 10 },
                    { "documentUpload", new BsonDocument
                        {
                            { "default", defaultUploadLimit },
                            { "other", otherUploadLimit }
                        }
                    },
                    { "createdBy", BsonNull.Value },
                    { "createdAt", now },
                    { "updatedBy", BsonNull.Value },
                    { "updatedAt", now }
                };

                await collection.InsertOneAsync(doc);
                Console.WriteLine("✅ Seeded default Settings document");
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (await CollectionExistsAsync(database, "Settings"))
            {
                await database.DropCollectionAsync("Settings");
                Console.WriteLine("✅ Dropped Settings collection");
            }
        }
    }
}
