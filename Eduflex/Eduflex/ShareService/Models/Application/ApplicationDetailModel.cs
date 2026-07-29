using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Address;
using ShareService.Models.Enrolment;

namespace ShareService.Models.Application
{
    public class ApplicationDetailModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("studentId")]
        public string StudentId { get; set; } = string.Empty;

        [BsonElement("StudentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("DateApplied")]
        public DateTime DateApplied { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("Details")]
        public string Details { get; set; } = string.Empty;

        [BsonElement("ApplicationType")]
        public string ApplicationType { get; set; } = string.Empty;

        [BsonElement("StudyMode")]
        public string? StudyMode { get; set; }

        [BsonElement("Campus")]
        public string? Campus { get; set; }

        [BsonElement("HometownAddress")]
        public AddressModel? HometownAddress { get; set; }

        [BsonElement("CurrentAddress")]
        public AddressModel? CurrentAddress { get; set; }

        [BsonElement("EmergencyContact")]
        public EmergencyContactModel? EmergencyContact { get; set; }
    }
}