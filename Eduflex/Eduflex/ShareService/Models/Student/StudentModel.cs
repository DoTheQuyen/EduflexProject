using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Student;
using ShareService.Models.Address;
using ShareService.Models.Application;
using ShareService.Models.Common;
using ShareService.Models.Education;

namespace ShareService.Models.Student
{
    public class StudentModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        // Student vs Customer (migration-case-only contact) — same record shape, this is
        // the only thing that distinguishes them. Immutable after creation (see
        // UpdateStudentDto) so a record can't silently flip type.
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public PersonType Type { get; set; } = PersonType.Student;

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("nationality")]
        public string Nationality { get; set; }

        [BsonElement("passportNumber")]
        public string PassportNumber { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("address")]
        public AddressModel Address { get; set; }

        [BsonElement("educationBackground")]
        public List<EducationModel> EducationBackground { get; set; } = new List<EducationModel>();

        [BsonElement("applications")]
        public List<ApplicationModel> Applications { get; set; } = new List<ApplicationModel>();

        [BsonElement("auditTrail")]
        public List<StudentAuditEntryModel> AuditTrail { get; set; } = new();

    }
}