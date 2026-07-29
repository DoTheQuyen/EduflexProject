namespace Eduflex.DTOs.BusinessPartner
{
    public class CreateBusinessPartnerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Trademark { get; set; }
        public string? Address { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Abn { get; set; }
        public string? Acn { get; set; }
        public decimal CommissionBaseRate { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string? ContractFileUrl { get; set; }
        public string? ContractFileName { get; set; }
        public List<BusinessPartnerContactDto> Contacts { get; set; } = new();
    }
}
