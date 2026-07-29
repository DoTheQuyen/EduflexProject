using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.Course
{
    [BsonIgnoreExtraElements]
    public class CourseModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("educationPartnerId")]
        public string EducationPartnerId { get; set; }

        [BsonElement("courseName")]
        public string CourseName { get; set; }

        [BsonElement("intakes")]
        public List<string> Intakes { get; set; } = new();

        [BsonElement("studyModes")]
        public List<string> StudyModes { get; set; } = new();

        [BsonElement("campuses")]
        public List<string> Campuses { get; set; } = new();

        // Per-annum tuition fee.
        [BsonElement("tuitionFee")]
        public decimal TuitionFee { get; set; }

        // Total tuition for the whole course (all years/terms combined), entered
        // independently rather than derived from TuitionFee * duration, since course
        // pricing isn't always a flat multiple of the annual rate.
        [BsonElement("totalTuitionFee")]
        public decimal TotalTuitionFee { get; set; }

        [BsonElement("tuitionCurrency")]
        public string TuitionCurrency { get; set; } = "AUD";

        [BsonElement("courseDurationMonths")]
        public int? CourseDurationMonths { get; set; }

        // Defaults from the parent EducationPartner's CommissionBaseRate at course-create
        // time (client-side prefill), but stored independently so it can be edited per
        // course afterwards without affecting the partner's own rate.
        [BsonElement("commissionBaseRate")]
        public decimal CommissionBaseRate { get; set; }
    }
}
