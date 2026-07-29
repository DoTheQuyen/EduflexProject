using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // EnrolmentEnums was reworked from the original 7-value set
    // (Draft/Submitted/OfferSent/Accepted/Enrolled/Deferred/Withdrawn) to the new
    // Offer -> COE -> Apply VISA -> VISA success/fail pipeline
    // (Draft/Offer/Coe/ApplyVisa/VisaSuccess/VisaFail/Cancel). Enrolments.status is a
    // raw string, not a native Mongo enum, so existing documents keep their old values
    // unless remapped here — otherwise the new UI won't recognise them.
    public class _026_MigrateEnrolmentStatusValues_280726 : SafeMigrationBase
    {
        public override string MigrationId => "_026_MigrateEnrolmentStatusValues_280726";
        public override string Name => "Migrate Enrolment status values to the new enum";
        public override string Description => "Remaps existing Enrolments.status string values from the old EnrolmentEnums set to the new Offer/COE/ApplyVisa/VisaSuccess/VisaFail/Cancel vocabulary";

        // Old value -> new value. "Draft" is unchanged so it's omitted.
        private static readonly Dictionary<string, string> StatusMap = new()
        {
            ["Submitted"] = "Draft",
            ["OfferSent"] = "Offer",
            ["Accepted"] = "Coe",
            ["Enrolled"] = "VisaSuccess",
            ["Deferred"] = "Cancel",
            ["Withdrawn"] = "Cancel",
        };

        public override async Task Up(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Enrolments"))
            {
                Console.WriteLine("⚠️ Enrolments collection doesn't exist. Skipping.");
                return;
            }

            var enrolmentsCollection = database.GetCollection<BsonDocument>("Enrolments");

            foreach (var (oldValue, newValue) in StatusMap)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("status", oldValue);
                var update = Builders<BsonDocument>.Update.Set("status", newValue);
                var result = await enrolmentsCollection.UpdateManyAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    Console.WriteLine($"✅ Remapped {result.ModifiedCount} enrolment(s) from status \"{oldValue}\" to \"{newValue}\"");
                }
            }
        }

        public override Task Down(IMongoDatabase database)
        {
            // Not safely reversible — both "Deferred" and "Withdrawn" collapse onto
            // "Cancel" going forward, so the original distinction can't be reconstructed.
            Console.WriteLine("⚠️ This migration cannot be automatically reversed: \"Deferred\" and \"Withdrawn\" both mapped onto \"Cancel\" and that distinction is lost. Restore from a backup if you need to roll back.");
            return Task.CompletedTask;
        }
    }
}
