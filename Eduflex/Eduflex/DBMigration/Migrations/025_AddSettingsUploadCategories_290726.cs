using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _025_AddSettingsUploadCategories_290726 : SafeMigrationBase
    {
        public override string MigrationId => "_025_AddSettingsUploadCategories_290726";
        public override string Name => "Add Image/Contract/Enrolment Upload Settings";
        public override string Description => "Adds imageUpload, contractUpload, and enrolmentUpload limit fields to the existing singleton Settings document, seeded with the values previously hardcoded across education-partner-edit (logo), feedback-management (photo), business-partner-edit (contract), and the enrolment documents/visa tabs.";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Settings"))
            {
                Console.WriteLine("⚠️ Settings collection doesn't exist. Run migration 023 first. Skipping.");
                return;
            }

            var updates = new List<UpdateDefinition<BsonDocument>>();

            if (!await FieldExistsAsync(database, "Settings", "imageUpload"))
            {
                updates.Add(Builders<BsonDocument>.Update.Set("imageUpload", new BsonDocument
                {
                    { "maxSizeMB", 2.0 },
                    { "allowedExtensions", new BsonArray { ".jpg", ".jpeg", ".png", ".gif", ".webp" } },
                    { "maxFileCount", 1 }
                }));
            }

            if (!await FieldExistsAsync(database, "Settings", "contractUpload"))
            {
                updates.Add(Builders<BsonDocument>.Update.Set("contractUpload", new BsonDocument
                {
                    { "maxSizeMB", 10.0 },
                    { "allowedExtensions", new BsonArray { ".pdf", ".doc", ".docx" } },
                    { "maxFileCount", 1 }
                }));
            }

            if (!await FieldExistsAsync(database, "Settings", "enrolmentUpload"))
            {
                updates.Add(Builders<BsonDocument>.Update.Set("enrolmentUpload", new BsonDocument
                {
                    { "maxSizeMB", 10.0 },
                    { "allowedExtensions", new BsonArray() },
                    { "maxFileCount", 1 }
                }));
            }

            if (updates.Count == 0)
            {
                Console.WriteLine("✅ Settings already has all upload category fields");
                return;
            }

            var collection = database.GetCollection<BsonDocument>("Settings");
            await collection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Combine(updates));
            Console.WriteLine($"✅ Added {updates.Count} new upload category field(s) to Settings");
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Settings"))
            {
                return;
            }

            var collection = database.GetCollection<BsonDocument>("Settings");
            var unset = Builders<BsonDocument>.Update.Combine(
                Builders<BsonDocument>.Update.Unset("imageUpload"),
                Builders<BsonDocument>.Update.Unset("contractUpload"),
                Builders<BsonDocument>.Update.Unset("enrolmentUpload"));
            await collection.UpdateManyAsync(new BsonDocument(), unset);
            Console.WriteLine("✅ Removed upload category fields from Settings");
        }
    }
}
