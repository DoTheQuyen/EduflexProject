using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Common;

namespace ShareService.Models.EducationPartner
{
    [BsonIgnoreExtraElements]
    public class EducationPartnerModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("trademark")]
        public string Trademark { get; set; }

        [BsonElement("logoUrl")]
        public string LogoUrl { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }

        [BsonElement("partnerType")]
        public string PartnerType { get; set; }

        [BsonElement("intakes")]
        public List<string> Intakes { get; set; } = new();

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        // Optional — some education partners contract directly with Eduflex rather than
        // through a business partner, so this link ("Managed under partner" in the UI) is
        // nullable, not a required relationship.
        [BsonElement("businessPartnerId")]
        public string? BusinessPartnerId { get; set; }

        [BsonElement("commissionBaseRate")]
        public decimal CommissionBaseRate { get; set; }

        [BsonElement("abn")]
        public string? Abn { get; set; }

        [BsonElement("acn")]
        public string? Acn { get; set; }

        public void ApplyEditableFields(EducationPartnerModel updateModel)
        {
            Name = updateModel.Name;
            Location = updateModel.Location;
            Trademark = updateModel.Trademark;
            LogoUrl = updateModel.LogoUrl;
            Description = updateModel.Description;
            Country = updateModel.Country;
            PartnerType = updateModel.PartnerType;
            Intakes = updateModel.Intakes;
            Email = updateModel.Email;
            PhoneNumber = updateModel.PhoneNumber;
            BusinessPartnerId = updateModel.BusinessPartnerId;
            CommissionBaseRate = updateModel.CommissionBaseRate;
            Abn = updateModel.Abn;
            Acn = updateModel.Acn;
        }
    }
}
