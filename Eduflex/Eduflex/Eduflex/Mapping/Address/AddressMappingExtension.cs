using Eduflex.DTOs.Address;
using ShareService.Models.Address;

namespace Eduflex.Mapping.Address
{
    public static class AddressMappingExtension
    {
        public static AddressDto ToDto(this AddressModel model)
        {
            return new AddressDto
            {
                Street = model.Street,
                Suburb = model.Suburb,
                City = model.City,
                State = model.State,
                Country = model.Country,
                PostalCode = model.PostalCode
            };
        }

        public static AddressModel ToModel(this AddressDto dto)
        {
            return new AddressModel
            {
                Street = dto.Street,
                Suburb = dto.Suburb,
                City = dto.City,
                State = dto.State,
                Country = dto.Country,
                PostalCode = dto.PostalCode
            };
        }
    }
}