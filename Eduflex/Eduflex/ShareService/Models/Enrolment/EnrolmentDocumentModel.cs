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

        // Set only for documents attached to one specific course application (currently
        // just the "UniOffer" category — a student can have several parallel course
        // applications, each with its own offer letter). Null for enrolment-wide
        // documents (GS, CoE, VisaDraft, etc.) that aren't tied to a single application.
        [BsonElement("courseApplicationId")]
        public string? CourseApplicationId { get; set; }

        // Free-text note staff can attach to any uploaded file (e.g. "second page of the
        // offer letter", "receipt for the July instalment") — shown alongside the file in
        // the uploader zone. Optional, no validation beyond a max length on the DTO.
        [BsonElement("note")]
        public string? Note { get; set; }

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
