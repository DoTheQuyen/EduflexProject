using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Address;
using ShareService.Models.Common;
using ShareService.Models.Enrolment;

namespace ShareService.Models.Application
{
    public class ApplicationModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("studentId")]
        public string StudentId { get; set; } = string.Empty;

        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("studentEmail")]
        public string StudentEmail { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("details")]
        public string Details { get; set; }

        [BsonElement("applicationType")]
        public string ApplicationType { get; set; }

        [BsonElement("studyMode")]
        public string? StudyMode { get; set; }

        [BsonElement("campus")]
        public string? Campus { get; set; }

        [BsonElement("dateApplied")]
        public DateTime DateApplied { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Pending";

        // These reflect the student's situation for THIS application specifically —
        // deliberately not on StudentModel, since address/emergency contact can
        // legitimately differ between separate applications for the same student.
        [BsonElement("hometownAddress")]
        public AddressModel? HometownAddress { get; set; }

        [BsonElement("currentAddress")]
        public AddressModel? CurrentAddress { get; set; }

        [BsonElement("emergencyContact")]
        public EmergencyContactModel? EmergencyContact { get; set; }
    }
}
