using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Notification
{
    [BsonIgnoreExtraElements]
    public class NotificationModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("module")]
        public string Module { get; set; } = string.Empty;

        [BsonElement("entityId")]
        public string EntityId { get; set; } = string.Empty;

        [BsonElement("summary")]
        public string Summary { get; set; } = string.Empty;

        // NotificationTargetType, stored as its string name — Department / DepartmentHead / Staff.
        [BsonElement("targetType")]
        public string TargetType { get; set; } = string.Empty;

        // Set when TargetType is Department or DepartmentHead; null for Staff. Kept for
        // display/audit ("this was sent to Finance") — recipient resolution itself reads
        // RecipientUserIds below, not this.
        [BsonElement("targetDepartmentId")]
        public string? TargetDepartmentId { get; set; }

        // The resolved recipient snapshot at publish time — who actually gets this
        // notification. Resolved once (department membership / head / explicit staff list)
        // rather than re-resolved on every read, so a later membership change doesn't
        // retroactively change who already-sent notifications belong to.
        [BsonElement("recipientUserIds")]
        public List<string> RecipientUserIds { get; set; } = new();

        // Per-user clear state — a colleague clearing their copy doesn't affect anyone
        // else's, since each user id is tracked independently here.
        [BsonElement("clearedByUserIds")]
        public List<string> ClearedByUserIds { get; set; } = new();
    }
}
