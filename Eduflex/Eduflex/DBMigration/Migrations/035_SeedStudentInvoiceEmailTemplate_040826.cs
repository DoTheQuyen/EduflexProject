using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // The existing "invoice-notification" template (migration 027) is worded for the
    // Financial module's partner commission invoices ("Please find attached commission
    // invoice..."), which reads wrong when reused for a student service-fee invoice — this
    // seeds a distinct, correctly-worded template for that flow instead, following the
    // same {{token}} convention as the other seeded templates (e.g. migration 030's
    // dynamic-form-request).
    public class _035_SeedStudentInvoiceEmailTemplate_040826 : SafeMigrationBase
    {
        public override string MigrationId => "_035_SeedStudentInvoiceEmailTemplate_040826";
        public override string Name => "Seed student invoice email template";
        public override string Description => "Adds a 'student-invoice-notification' email template, distinct from the partner-facing 'invoice-notification' one";

        private const string TemplateKey = "student-invoice-notification";

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("EmailTemplates");

            var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("key", TemplateKey)).FirstOrDefaultAsync();
            if (existing != null)
            {
                Console.WriteLine("✅ Student invoice email template already exists — skipping.");
                return;
            }

            var now = DateTime.UtcNow;
            var template = new BsonDocument
            {
                { "key", TemplateKey },
                { "name", "Invoice Notification (Student)" },
                { "subject", "Your Eduflex invoice {{invoiceNo}}" },
                { "body", "Hello {{studentFirstName}},\n\nPlease find your invoice {{invoiceNo}} for {{invoiceDescription}}.\n\nDownload link: {{invoiceLink}}\n\nKind regards,\n{{staffName}}" },
                { "isSystemDefault", true },
                { "createdBy", BsonNull.Value },
                { "createdAt", now },
                { "updatedBy", BsonNull.Value },
                { "updatedAt", now }
            };

            await collection.InsertOneAsync(template);
            Console.WriteLine("✅ Seeded 'student-invoice-notification' email template");
        }

        public override async Task Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("EmailTemplates");
            var result = await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("key", TemplateKey));
            Console.WriteLine(result.DeletedCount > 0 ? "✅ Removed student invoice email template" : "⚠️ Student invoice email template not found");
        }
    }
}
