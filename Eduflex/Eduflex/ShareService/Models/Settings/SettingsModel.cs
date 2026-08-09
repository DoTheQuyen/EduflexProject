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

        // System-wide ceiling on how many concurrent course applications a student's
        // enrolment can hold — see EnrolmentModel.MaxApplicationsOverride, which lets
        // staff dial an individual student's allowance anywhere up to this value, but
        // never past it.
        [BsonElement("maxApplicationsPerStudent")]
        public int MaxApplicationsPerStudent { get; set; } = 1;

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

        // Visa Q&A chat widget config — DB-backed so the prompt/endpoint can be tuned
        // without a redeploy. The Gemini API key deliberately stays out of this document
        // (see GeminiSettings/user-secrets) since it's a secret, not app config.
        [BsonElement("chatSystemPrompt")]
        public string ChatSystemPrompt { get; set; } =
            "You are a general information assistant helping international students understand Australian visa subclasses (e.g. 485 Temporary Graduate, 189 Skilled Independent, 190 Skilled Nominated, 491 Skilled Work Regional). " +
            "Rules you must follow: " +
            "1. ONLY answer questions related to Australian student/skilled visas, migration pathways, and studying in Australia. If a question is unrelated to these topics, politely say you can only help with visa and study-related questions, and suggest they contact the team directly for anything else. Do not answer general knowledge, coding, or unrelated questions. " +
            "2. Provide GENERAL INFORMATION only, never migration advice. You are not a Registered Migration Agent and cannot assess anyone's individual case. " +
            "3. Always remind the student to confirm current details on the official Department of Home Affairs website (immi.homeaffairs.gov.au), since visa rules change. " +
            "4. If a question needs personal case assessment, say so and recommend a Registered Migration Agent instead of guessing. " +
            "5. Write in plain, friendly, conversational text suitable for a chat message — like you're texting a friend, not writing a report. Do NOT use markdown formatting: no headers (#), no bold asterisks (**), no horizontal rules (---). Keep paragraphs short. You may use a simple dash and line break for short lists, but nothing fancier than that.";

        // {model} is substituted at call time with GeminiSettings.Model; the API key is
        // appended separately from the secret store, never stored in this document.
        [BsonElement("chatApiUrl")]
        public string ChatApiUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
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
