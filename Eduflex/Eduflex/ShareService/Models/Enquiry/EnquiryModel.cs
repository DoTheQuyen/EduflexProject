using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Enums.Roles;
using ShareService.Models.Common;
using ShareService.Models.CoursePromotion;

namespace ShareService.Models.Enquiry
{
    public class EnquiryModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("middleName")]
        public string? MiddleName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("mobile")]
        public string Mobile { get; set; }

        [BsonElement("enquiry")]
        public string Enquiry { get; set; }

        [BsonElement("subject")]
        public string Subject { get; set; } = "General Enquiry";

        [BsonElement("coursePromotionId")]
        public string? CoursePromotionId { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = EnquiryEnums.New.ToString();

        [BsonElement("response")]
        public string? Response { get; set; }

        [BsonIgnore]
        public string RecaptchaToken { get; set; } = string.Empty;

        public void ApplyEditableFields(EnquiryModel updateModel)
        {
            // Subject and CoursePromotionId are set once at creation and never editable by staff.
            FirstName = updateModel.FirstName;
            MiddleName = updateModel.MiddleName;
            LastName = updateModel.LastName;
            Email = updateModel.Email;
            Mobile = updateModel.Mobile;
            Enquiry = updateModel.Enquiry;
            Status = updateModel.Status;
            Response = updateModel.Response;
        }
    }
}