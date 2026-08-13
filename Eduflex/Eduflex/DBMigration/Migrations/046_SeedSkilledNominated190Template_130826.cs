using DBMigration.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Seeds a second VisaProcessTemplates document — "Australia — Skilled Nominated (190)" —
    // so the Process Designer has more than one category to show. Grounded in
    // docs/09-visa-process-config-module-design.md Part F.2: no course, no provider, no CoE
    // — the spine is a points-tested EOI via SkillSelect, gated by a state/territory
    // nomination (which is what distinguishes 190 from 189). 190 is a direct-to-permanent
    // visa, so unlike the 491 template sketched in the mockup this one has no provisional
    // regional-residence/income-tracking compliance phase. Purely additive, same as
    // migration 045 — not wired into EnrolmentService or the live VISA Process tab.
    public class _046_SeedSkilledNominated190Template_130826 : SafeMigrationBase
    {
        public override string MigrationId => "_046_SeedSkilledNominated190Template_130826";
        public override string Name => "Seed Skilled Nominated (190) VISA process template";
        public override string Description => "Seeds a second VisaProcessTemplates document — Australia — Skilled Nominated (190) — demonstrating a second Country+Category alongside the seeded Student default";

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "VisaProcessTemplates"))
            {
                Console.WriteLine("⚠️ VisaProcessTemplates collection doesn't exist. Run migration 045 first. Skipping.");
                return;
            }

            var templatesCollection = database.GetCollection<BsonDocument>("VisaProcessTemplates");

            var existing = await templatesCollection.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("country", "AU"),
                Builders<BsonDocument>.Filter.Eq("category", "SkilledNominated190")
            )).FirstOrDefaultAsync();

            if (existing != null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var steps = new BsonArray
            {
                Step("SkillsAssessment", 0, "Skills Assessment", "Positive assessment from the occupation's assessing authority — required before EOI.", "Eligibility",
                    evidence: new[] { "SkillsAssessmentLetter" },
                    fields: new BsonArray
                    {
                        Field("occupation", "Nominated occupation", "Text", required: true),
                        Field("assessingAuthority", "Assessing authority", "Text", required: true),
                        Field("assessmentOutcome", "Outcome", "Select", required: true, options: new[] { "Positive", "Negative" })
                    }),
                Step("EnglishTestEvidence", 1, "English Test Evidence", "Points-relevant English test result.", "Eligibility",
                    evidence: new[] { "EnglishTestResult" },
                    fields: new BsonArray { Field("testType", "Test type", "Text", required: true), Field("score", "Score", "Number", required: true) }),
                Step("EoiSubmission", 2, "EOI Submission", "Expression of Interest lodged via SkillSelect, carrying the points score.", "Application",
                    fields: new BsonArray
                    {
                        Field("pointsScore", "Points score", "Number", required: true),
                        Field("eoiDate", "EOI date", "Date", required: true)
                    }),
                Step("StateNomination", 3, "State Nomination", "State/territory nomination — required for 190; this is what distinguishes it from 189.", "Application",
                    evidence: new[] { "NominationApproval" },
                    fields: new BsonArray
                    {
                        Field("state", "Nominating state", "Text", required: true),
                        Field("nominationOutcome", "Outcome", "Select", required: true, options: new[] { "Approved", "Refused" })
                    }),
                Step("Invitation", 4, "Invitation", "Invitation received from the EOI pool.", "Application",
                    canReopen: false,
                    evidence: new[] { "InvitationLetter" },
                    fields: new BsonArray { Field("invitationRound", "Invitation round", "Text", required: true), Field("expiryDate", "Response deadline", "Date", required: true) }),
                Step("VisaLodgementSkilled", 5, "Visa Lodgement", "Full application with health and character checks.", "Application",
                    evidence: new[] { "VisaPaymentReceipt", "PoliceCheck", "HealthExam" },
                    fields: new BsonArray { Field("lodgedDate", "Lodged date", "Date", required: true), Field("applicationId", "Application ID", "Text", required: true) }),
                Step("OutcomeSkilled", 6, "Outcome", "Grant or refusal — 190 is a direct-to-permanent visa, no provisional stage.", "Application",
                    canReopen: false,
                    evidence: new[] { "VisaGranted" },
                    fields: new BsonArray { Field("outcome", "Outcome", "Select", required: true, options: new[] { "Granted", "Refused" }) },
                    preconditions: new BsonArray { Precondition("FieldValueIn", fieldKey: "outcome", allowedValues: new[] { "Granted", "Refused" }, detail: "outcome must be exactly Granted or Refused before this step can complete.") })
            };

            var template = new BsonDocument
            {
                { "name", "Australia — Skilled Nominated (190)" },
                { "country", "AU" },
                { "category", "SkilledNominated190" },
                { "description", "No course, no provider, no CoE — the spine is a points-tested EOI gated by state nomination. Direct to permanent residence." },
                { "status", "Active" },
                { "isDefaultForCountry", true },
                { "version", 1 },
                { "steps", steps },
                { "createdAt", now },
                { "updatedAt", now }
            };

            await templatesCollection.InsertOneAsync(template);
            Console.WriteLine("✅ Seeded template: Australia — Skilled Nominated (190)");
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
            if (await CollectionExistsAsync(database, "VisaProcessTemplates"))
            {
                var templatesCollection = database.GetCollection<BsonDocument>("VisaProcessTemplates");
                await templatesCollection.DeleteOneAsync(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("country", "AU"),
                    Builders<BsonDocument>.Filter.Eq("category", "SkilledNominated190")
                ));
            }

            Console.WriteLine("✅ Rolled back Skilled Nominated (190) template seed");
        }
    }
}
