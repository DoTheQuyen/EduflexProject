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

        [BsonElement("status")]
        public string Status { get; set; } = EnquiryEnums.New.ToString();

        [BsonElement("response")]
        public string? Response { get; set; }

        /// <summary>
        /// Token returned by the Google reCAPTCHA v2 widget on the client, verified server-side but never persisted.
        /// </summary>
        [BsonIgnore]
        public string RecaptchaToken { get; set; } = string.Empty;

        public void ApplyEditableFields(EnquiryModel updateModel)
        {
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
