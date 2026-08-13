using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Creates the VisaProcessTemplates and PractitionerTags collections and seeds the
    // VisaProcessTemplatesEdit permission (Admin-only — see
    // docs/09-visa-process-config-module-design.md §C.6/§C.9 for why this is one flat key
    // covering both catalogs rather than a View/Add/Edit/Delete set or a second key). Also
    // seeds a single default template, "Australia — Standard", that is a byte-for-byte match
    // of today's hardcoded VisaProcessStepKeys (see
    // ShareService/Models/Enrolment/VisaProcessStepModel.cs) expressed in the new
    // step-definition shape — this module is NOT wired into EnrolmentService yet (see
    // docs/09 for the deferred integration phase), so seeding this template is purely
    // content for the new Process Designer screen; it has no effect on the live VISA
    // Process tab.
    public class _045_AddVisaProcessTemplatesAndPractitionerTags_130826 : SafeMigrationBase
    {
        public override string MigrationId => "_045_AddVisaProcessTemplatesAndPractitionerTags_130826";
        public override string Name => "Add VISA Process Templates + Practitioner Tags module";
        public override string Description => "Creates VisaProcessTemplates + PractitionerTags collections and indexes, seeds VisaProcessTemplatesEdit permission (Admin only) and the default Australia — Standard template";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "VisaProcessTemplates"))
            {
                await database.CreateCollectionAsync("VisaProcessTemplates");
                Console.WriteLine("✅ Created VisaProcessTemplates collection");
            }

            await CreateIndexSafeAsync(database, "VisaProcessTemplates",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("country").Ascending("category"),
                    new CreateIndexOptions { Name = "idx_country_category" }));

            await CreateIndexSafeAsync(database, "VisaProcessTemplates",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("status"),
                    new CreateIndexOptions { Name = "idx_status" }));

            if (!await CollectionExistsAsync(database, "PractitionerTags"))
            {
                await database.CreateCollectionAsync("PractitionerTags");
                Console.WriteLine("✅ Created PractitionerTags collection");
            }

            await CreateIndexSafeAsync(database, "PractitionerTags",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("active"),
                    new CreateIndexOptions { Name = "idx_active" }));

            await SeedPermissionAsync(database);
            await SeedDefaultTemplateAsync(database);
        }

        private async Task SeedPermissionAsync(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Roles"))
            {
                Console.WriteLine("⚠️ Roles collection doesn't exist. Run migrations 010/011 first. Skipping permission seed.");
                return;
            }

            var rolesCollection = database.GetCollection<BsonDocument>("Roles");
            var modulesCollection = database.GetCollection<BsonDocument>("Modules");
            var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

            var adminRole = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "Admin")).FirstOrDefaultAsync();
            if (adminRole == null)
            {
                Console.WriteLine("⚠️ Admin role not found. Run migration 010 first. Skipping permission seed.");
                return;
            }

            var module = await modulesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", "VisaProcessTemplates")).FirstOrDefaultAsync();
            if (module == null)
            {
                module = new BsonDocument
                {
                    { "name", "VisaProcessTemplates" },
                    { "description", "Configurable VISA process templates and practitioner routing tags" }
                };
                await modulesCollection.InsertOneAsync(module);
                Console.WriteLine("✅ Seeded VisaProcessTemplates module");
            }
            var moduleId = module["_id"].AsObjectId;

            var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "VisaProcessTemplatesEdit")).FirstOrDefaultAsync();
            if (permission == null)
            {
                permission = new BsonDocument
                {
                    { "moduleId", moduleId.ToString() },
                    { "action", "Edit" },
                    { "key", "VisaProcessTemplatesEdit" },
                    { "description", "Edit VISA Process Templates" }
                };
                await permissionsCollection.InsertOneAsync(permission);
                Console.WriteLine("✅ Seeded permission: VisaProcessTemplatesEdit");
            }

            var idString = permission["_id"].AsObjectId.ToString();
            var update = Builders<BsonDocument>.Update.AddToSet("permissionIds", idString);
            await rolesCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", adminRole["_id"].AsObjectId), update);
            Console.WriteLine("✅ Admin granted VISA Process Templates edit access");
        }

        private async Task SeedDefaultTemplateAsync(IMongoDatabase database)
        {
            var templatesCollection = database.GetCollection<BsonDocument>("VisaProcessTemplates");

            var existing = await templatesCollection.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("country", "AU"),
                Builders<BsonDocument>.Filter.Eq("category", "Student"),
                Builders<BsonDocument>.Filter.Eq("isDefaultForCountry", true)
            )).FirstOrDefaultAsync();

            if (existing != null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var steps = new BsonArray
            {
                Step("StudentInfo", 0, "Student Info", "Captured once at enrolment creation. Stored as typed EnrolmentModel fields, not this step's Fields bag — represented here purely for consistent numbering, labels and hints.", "Application", canReopen: false),
                Step("EnrolmentForm", 1, "Enrolment Form", "Agency service agreement + fee invoice, run in parallel with the provider offer.", "Application", canReopen: false, evidence: new[] { "GS" }),
                Step("ApplyOffer", 2, "Apply for Offer", "Submit course application(s); provider issues a Letter of Offer.", "Application",
                    evidence: new[] { "UniOffer" },
                    fields: new BsonArray { Field("offerAppliedDate", "Offer applied date", "Date", required: true), Field("applicationReference", "Application reference", "Text", required: false) },
                    preconditions: new BsonArray { Precondition("PriorStepFieldNotEmpty", sourceStepKey: "EnrolmentForm", fieldKey: "invoiceId", detail: "EnrolmentForm.fields.invoiceId must be set — service fee invoice sent before an offer can be lodged.") },
                    setsStatusTo: "Offer"),
                Step("CoeCompletion", 3, "CoE Completion", "Tuition deposit paid to the provider; provider issues the eCoE via PRISMS.", "Application",
                    evidence: new[] { "CoE", "PaymentReceipt" },
                    fields: new BsonArray { Field("coeNumber", "CoE number", "Text", required: true), Field("coeIssueDate", "CoE issue date", "Date", required: true) },
                    preconditions: new BsonArray { Precondition("CourseApplicationFinalized", detail: "At least one course application must have status Finalized.") },
                    setsStatusTo: "Coe"),
                Step("VisaApplication", 4, "Visa Application", "ImmiAccount lodgement, GS answers, biometrics/health exam booking, OSHC arranged, visa fee paid.", "Application",
                    evidence: new[] { "VisaDraft", "Insurance", "VisaPaymentReceipt" },
                    fields: new BsonArray { Field("applicationLodgedDate", "Lodged date", "Date", required: true), Field("visaApplicationId", "Visa application ID", "Text", required: true), Field("bioMedicalTestDate", "Health exam date", "Date", required: false) },
                    setsStatusTo: "ApplyVisa"),
                // VisaOutcome sets EnrolmentEnums to VisaSuccess or VisaFail depending on the
                // "outcome" field value at completion time — a dynamic mapping the static
                // SetsEnrolmentStatusTo (single value) can't express, so it's left null here
                // and documented as a known special case for whenever this module is wired
                // into EnrolmentService (see docs/09 §C.5).
                Step("VisaOutcome", 5, "Visa Outcome", "Grant or refusal recorded; on Granted, linked application flips to Studying.", "Application",
                    canReopen: false,
                    evidence: new[] { "VisaGranted" },
                    fields: new BsonArray
                    {
                        Field("outcome", "Outcome", "Select", required: true, options: new[] { "Granted", "Refused" }),
                        Field("visaGrantNumber", "Grant number", "Text", required: false),
                        Field("visaExpiryDate", "Visa expiry", "Date", required: false)
                    },
                    preconditions: new BsonArray { Precondition("FieldValueIn", fieldKey: "outcome", allowedValues: new[] { "Granted", "Refused" }, detail: "outcome must be exactly Granted or Refused before this step can complete.") })
            };

            var template = new BsonDocument
            {
                { "name", "Australia — Standard" },
                { "country", "AU" },
                { "category", "Student" },
                { "description", "The seeded default — a 1:1 match of the 6 steps already live on the VISA Process tab today." },
                { "status", "Active" },
                { "isDefaultForCountry", true },
                { "version", 1 },
                { "steps", steps },
                { "createdAt", now },
                { "updatedAt", now }
            };

            await templatesCollection.InsertOneAsync(template);
            Console.WriteLine("✅ Seeded default template: Australia — Standard");
        }

        private static BsonDocument Step(
            string key, int order, string label, string description, string phase,
            bool canReopen = true, string[]? evidence = null, BsonArray? fields = null,
            BsonArray? preconditions = null, string? setsStatusTo = null)
        {
            return new BsonDocument
            {
                { "key", key },
                { "order", order },
                { "label", label },
                { "description", description },
                { "phase", phase },
                { "enabled", true },
                { "canReopen", canReopen },
                { "practitionerTagId", BsonNull.Value },
                { "fields", fields ?? new BsonArray() },
                { "requiredEvidenceCategories", evidence != null ? new BsonArray(evidence) : new BsonArray() },
                { "preconditions", preconditions ?? new BsonArray() },
                { "setsEnrolmentStatusTo", setsStatusTo != null ? setsStatusTo : BsonNull.Value },
                { "hints", new BsonArray() }
            };
        }

        private static BsonDocument Field(string fieldKey, string label, string inputType, bool required, string[]? options = null)
        {
            return new BsonDocument
            {
                { "fieldKey", fieldKey },
                { "label", label },
                { "inputType", inputType },
                { "options", options != null ? new BsonArray(options) : new BsonArray() },
                { "isRequired", required }
            };
        }

        private static BsonDocument Precondition(string type, string? sourceStepKey = null, string? fieldKey = null, string[]? allowedValues = null, string? detail = null)
        {
            return new BsonDocument
            {
                { "type", type },
                { "sourceStepKey", sourceStepKey != null ? sourceStepKey : BsonNull.Value },
                { "fieldKey", fieldKey != null ? fieldKey : BsonNull.Value },
                { "allowedValues", allowedValues != null ? new BsonArray(allowedValues) : new BsonArray() },
                { "detail", detail != null ? detail : BsonNull.Value }
            };
        }

        public override async Task Down(IMongoDatabase database)
        {
            await DropIndexSafeAsync(database, "VisaProcessTemplates", "idx_country_category");
            await DropIndexSafeAsync(database, "VisaProcessTemplates", "idx_status");
            await DropIndexSafeAsync(database, "PractitionerTags", "idx_active");

            if (await CollectionExistsAsync(database, "Roles"))
            {
                var rolesCollection = database.GetCollection<BsonDocument>("Roles");
                var modulesCollection = database.GetCollection<BsonDocument>("Modules");
                var permissionsCollection = database.GetCollection<BsonDocument>("Permissions");

                var permission = await permissionsCollection.Find(Builders<BsonDocument>.Filter.Eq("key", "VisaProcessTemplatesEdit")).FirstOrDefaultAsync();
                if (permission != null)
                {
                    var idString = permission["_id"].AsObjectId.ToString();
                    await rolesCollection.UpdateManyAsync(new BsonDocument(), Builders<BsonDocument>.Update.Pull("permissionIds", idString));
                    await permissionsCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"].AsObjectId));
                }

                await modulesCollection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("name", "VisaProcessTemplates"));
            }

            Console.WriteLine("✅ Rolled back VISA Process Templates + Practitioner Tags module (collections themselves left in place — drop manually if truly needed)");
        }
    }
}
