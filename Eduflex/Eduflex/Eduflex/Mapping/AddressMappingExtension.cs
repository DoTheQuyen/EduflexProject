using Eduflex.API.DTOs;
using ShareService.Models.Address;

namespace Eduflex.API.Mapping
{
    public static class AddressMappingExtension
    {
        public static AddressDto ToDto(this AddressModel model)
        {
            return new AddressDto
            {
                Street = model.Street,
                City = model.City,
                Country = model.Country,
                PostalCode = model.PostalCode
            };
        }

        public static AddressModel ToModel(this AddressDto dto)
        {
            return new AddressModel
            {
                Street = dto.Street,
                City = dto.City,
                Country = dto.Country,
                PostalCode = dto.PostalCode
            };
        }
    }
}