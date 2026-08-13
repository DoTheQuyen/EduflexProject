using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Task;

namespace ShareService.Models.Task
{
    // Append-only sub-collection embedded on TaskItemModel — same shape as
    // EnrolmentAuditEntryModel/EnrolmentCommunicationModel: own Guid id, a denormalized
    // "who" snapshot (CreatedByUserId/CreatedByName) instead of joining to Users, and a
    // timestamp. Notes are never edited or removed once added, per the module spec.
    public class TaskNoteModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("type")]
        public string Type { get; set; } = TaskNoteType.Note.ToString();

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("createdByUserId")]
        public string? CreatedByUserId { get; set; }

        [BsonElement("createdByName")]
        public string CreatedByName { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static TaskNoteModel Create(TaskNoteType type, string content, string? userId, string userName) => new()
        {
            Type = type.ToString(),
            Content = content,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow
        };
    }
}
