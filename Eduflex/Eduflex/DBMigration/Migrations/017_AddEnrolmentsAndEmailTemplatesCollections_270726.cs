using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _017_AddEnrolmentsAndEmailTemplatesCollections_270726 : SafeMigrationBase
    {
        public override string MigrationId => "_017_AddEnrolmentsAndEmailTemplatesCollections_270726";
        public override string Name => "Add Enrolments and EmailTemplates Collections";
        public override string Description => "Create the Enrolments collection (staff-owned student enrolment records) and EmailTemplates collection, plus seed default templates";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Enrolments"))
            {
                await database.CreateCollectionAsync("Enrolments");
                Console.WriteLine("✅ Created Enrolments collection");
            }

            await CreateIndexSafeAsync(database, "Enrolments",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("ownerUserId"),
                    new CreateIndexOptions { Name = "idx_ownerUserId" }));

            await CreateIndexSafeAsync(database, "Enrolments",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("studentUserId"),
                    new CreateIndexOptions { Name = "idx_studentUserId" }));

            await CreateIndexSafeAsync(database, "Enrolments",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("status"),
                    new CreateIndexOptions { Name = "idx_status" }));

            if (!await CollectionExistsAsync(database, "EmailTemplates"))
            {
                await database.CreateCollectionAsync("EmailTemplates");
                Console.WriteLine("✅ Created EmailTemplates collection");
            }

            var templatesCollection = database.GetCollection<BsonDocument>("EmailTemplates");
            var defaults = new (string Key, string Name, string Subject, string Body)[]
            {
                ("welcome", "Welcome & login details",
                    "Welcome to Eduflex — your login details",
                    "Hi {{studentFirstName}},\n\nWelcome to {{university}}! Your student portal account has been created — you can now log in to upload documents and track your enrolment.\n\nLogin email: {{studentEmail}}\nSet your password: (link sent separately)\n\nKind regards,\n{{staffName}}"),
                ("document-request", "Document request",
                    "A few more documents needed",
                    "Hi {{studentFirstName}},\n\nThanks for the documents so far. To keep your enrolment moving, could you please also upload any outstanding items via your student portal.\n\nKind regards,\n{{staffName}}"),
                ("offer-letter", "Offer letter",
                    "Your offer letter — {{course}}",
                    "Hi {{studentFirstName}},\n\nCongratulations! Please find attached your offer letter for {{course}}, commencing {{intake}}.\n\nPlease review and accept via your student portal within 14 days.\n\nKind regards,\n{{staffName}}"),
                ("enrolment-confirmation", "Enrolment confirmation",
                    "Your enrolment is confirmed",
                    "Hi {{studentFirstName}},\n\nYour enrolment into {{course}} at {{university}} is now confirmed for {{intake}}.\n\nWe look forward to welcoming you.\n\nKind regards,\n{{staffName}}"),
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
                        { "createdAt", DateTime.UtcNow },
                        { "updatedAt", DateTime.UtcNow }
                    });
                    Console.WriteLine($"✅ Seeded email template: {template.Key}");
                }
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "Enrolments", "idx_ownerUserId");
            await DropIndexSafeAsync(database, "Enrolments", "idx_studentUserId");
            await DropIndexSafeAsync(database, "Enrolments", "idx_status");
        }
    }
}
