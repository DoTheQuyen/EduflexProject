using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // The Financial module's Communication tab reuses the existing generic
    // EmailTemplate infrastructure (no new plumbing needed) — this just seeds one
    // default template for it. {{invoiceNo}}/{{invoiceLink}} are plain string tokens the
    // Communication tab's send flow can interpolate, matching the manual-string-replace
    // style already used elsewhere (e.g. "\n" -> "<br/>" in SendCommunicationAsync)
    // rather than a full templating engine.
    public class _027_SeedInvoiceEmailTemplate_280726 : SafeMigrationBase
    {
        public override string MigrationId => "_027_SeedInvoiceEmailTemplate_280726";
        public override string Name => "Seed Invoice email template";
        public override string Description => "Adds a default 'invoice-notification' email template for the Financial module's Communication tab";

        private const string TemplateKey = "invoice-notification";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("EmailTemplates");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("key", TemplateKey)).FirstOrDefaultAsync();
            if (existing != null)
            {
                Console.WriteLine("✅ Invoice email template already exists — skipping.");
                return;
            }

            var now = DateTime.UtcNow;
            var template = new BsonDocument
            {
                { "key", TemplateKey },
                { "name", "Invoice Notification" },
                { "subject", "Your commission invoice {{invoiceNo}}" },
                { "body", "Hello,\n\nPlease find attached commission invoice {{invoiceNo}} for the period covered.\n\nDownload link: {{invoiceLink}}\n\nThank you for your continued partnership with Eduflex.\n\nRegards,\nEduflex Finance Team" },
                { "isSystemDefault", true },
                { "createdBy", BsonNull.Value },
                { "createdAt", now },
                { "updatedBy", BsonNull.Value },
                { "updatedAt", now }
            };

            await collection.InsertOneAsync(template);
            Console.WriteLine("✅ Seeded 'invoice-notification' email template");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("EmailTemplates");
            var result = await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("key", TemplateKey));
            Console.WriteLine(result.DeletedCount > 0 ? "✅ Removed invoice email template" : "⚠️ Invoice email template not found");
        }
    }
}
