namespace Eduflex.DTOs.Address
{
    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}