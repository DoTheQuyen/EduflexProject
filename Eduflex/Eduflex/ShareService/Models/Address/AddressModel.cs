using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Address
{
    public class AddressModel
    {
        [BsonElement("street")]
        public string Street { get; set; } = string.Empty;

        [BsonElement("suburb")]
        public string? Suburb { get; set; }

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("state")]
        public string? State { get; set; }

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;

        [BsonElement("postalCode")]
        public string PostalCode { get; set; } = string.Empty;
    }
}
