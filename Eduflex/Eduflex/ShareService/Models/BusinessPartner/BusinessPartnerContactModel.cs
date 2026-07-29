using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.BusinessPartner
{
    public class BusinessPartnerContactModel
    {
        [BsonElement("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("contactNo")]
        public string ContactNo { get; set; } = string.Empty;
    }
}
