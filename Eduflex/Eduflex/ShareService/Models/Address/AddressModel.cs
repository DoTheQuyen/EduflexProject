using MongoDB.Bson.Serialization.Attributes;

namespace ShareService.Models.Address
{
    public class AddressModel
    {
        [BsonElement("street")]
        public string Street { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }

        [BsonElement("postalCode")]
        public string PostalCode { get; set; }
    }
}
