using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Enrolment
{
    public class EnrolmentDocumentModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("category")]
        public string? Category { get; set; }

        [BsonElement("url")]
        public string Url { get; set; } = string.Empty;

        [BsonElement("contentType")]
        public string? ContentType { get; set; }

        [BsonElement("sizeBytes")]
        public long SizeBytes { get; set; }

        [BsonElement("uploadedByUserId")]
        public string? UploadedByUserId { get; set; }

        [BsonElement("uploadedByName")]
        public string UploadedByName { get; set; } = string.Empty;

        [BsonElement("isFromStudent")]
        public bool IsFromStudent { get; set; }

        [BsonElement("uploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
