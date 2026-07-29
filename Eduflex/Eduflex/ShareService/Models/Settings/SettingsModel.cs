using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Settings
{
    // Singleton document — exactly one row in the "Settings" collection holds
    // every DB-backed app config value. Distinct from ShareService.Models.Setting
    // (singular), which still holds the appsettings.json-backed IOptions<T> POCOs
    // for bootstrap-level config (Mongo connection, JWT, etc.) that must stay out of the DB.
    [BsonIgnoreExtraElements]
    public class SettingsModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("feedbackDefaultLatestCount")]
        public int FeedbackDefaultLatestCount { get; set; } = 10;

        [BsonElement("coursePromotionDefaultLatestCount")]
        public int CoursePromotionDefaultLatestCount { get; set; } = 10;

        [BsonElement("documentUpload")]
        public DocumentUploadSettings DocumentUpload { get; set; } = new();

        // Logo (education partners) and photo (feedback) uploads.
        [BsonElement("imageUpload")]
        public UploadLimit ImageUpload { get; set; } = new()
        {
            MaxSizeMB = 2,
            AllowedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
            MaxFileCount = 1
        };

        // Business partner contract uploads.
        [BsonElement("contractUpload")]
        public UploadLimit ContractUpload { get; set; } = new()
        {
            MaxSizeMB = 10,
            AllowedExtensions = new List<string> { ".pdf", ".doc", ".docx" },
            MaxFileCount = 1
        };

        // Enrolment documents + visa-process step attachments — currently unrestricted by
        // type (empty AllowedExtensions means "accept anything", same convention the
        // frontend already uses: an empty/joined-empty accept list skips the type check).
        [BsonElement("enrolmentUpload")]
        public UploadLimit EnrolmentUpload { get; set; } = new()
        {
            MaxSizeMB = 10,
            AllowedExtensions = new List<string>(),
            MaxFileCount = 1
        };
    }

    public class DocumentUploadSettings
    {
        // Applies to every application-form document slot except "other".
        [BsonElement("default")]
        public UploadLimit Default { get; set; } = new()
        {
            MaxSizeMB = 5,
            AllowedExtensions = new List<string> { ".pdf", ".jpg", ".jpeg", ".png" },
            MaxFileCount = 1
        };

        // "Other Supporting Documents" allows multiple files, so it gets its own limit.
        [BsonElement("other")]
        public UploadLimit Other { get; set; } = new()
        {
            MaxSizeMB = 5,
            AllowedExtensions = new List<string> { ".pdf", ".jpg", ".jpeg", ".png" },
            MaxFileCount = 4
        };
    }

    public class UploadLimit
    {
        [BsonElement("maxSizeMB")]
        public double MaxSizeMB { get; set; }

        [BsonElement("allowedExtensions")]
        public List<string> AllowedExtensions { get; set; } = new();

        [BsonElement("maxFileCount")]
        public int MaxFileCount { get; set; }
    }
}
