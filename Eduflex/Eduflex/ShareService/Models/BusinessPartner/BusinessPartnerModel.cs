using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.BusinessPartner
{
    [BsonIgnoreExtraElements]
    public class BusinessPartnerModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("trademark")]
        public string? Trademark { get; set; }

        [BsonElement("address")]
        public string? Address { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("abn")]
        public string? Abn { get; set; }

        [BsonElement("acn")]
        public string? Acn { get; set; }

        [BsonElement("commissionBaseRate")]
        public decimal CommissionBaseRate { get; set; }

        [BsonElement("contractStartDate")]
        public DateTime? ContractStartDate { get; set; }

        [BsonElement("contractEndDate")]
        public DateTime? ContractEndDate { get; set; }

        [BsonElement("contractFileUrl")]
        public string? ContractFileUrl { get; set; }

        [BsonElement("contractFileName")]
        public string? ContractFileName { get; set; }

        [BsonElement("contacts")]
        public List<BusinessPartnerContactModel> Contacts { get; set; } = new();
    }
}
