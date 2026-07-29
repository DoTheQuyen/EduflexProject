using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Financial
{
    // Human-entered, additive line item — e.g. extra commission owed because a student
    // failed a semester and is now studying longer than originally planned. Deliberately
    // free-text + a signed amount rather than a formula, since this needs case-by-case
    // judgement, not automation.
    public class CommissionAdjustmentModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("addedByUserId")]
        public string? AddedByUserId { get; set; }

        [BsonElement("addedByName")]
        public string AddedByName { get; set; } = string.Empty;

        [BsonElement("addedAt")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
