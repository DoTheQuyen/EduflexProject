using Eduflex.DTOs.BusinessPartner;
using Eduflex.Mapping.BusinessPartner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Models.BusinessPartner;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessPartnersController : BaseApiController
    {
        private readonly IBusinessPartnerService _businessPartnerService;
        private readonly ILogger<BusinessPartnersController> _logger;

        public BusinessPartnersController(
            IBusinessPartnerService businessPartnerService,
            ILogger<BusinessPartnersController> logger)
        {
            _businessPartnerService = businessPartnerService;
            _logger = logger;
        }

        [HttpGet]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<BusinessPartnerDto>>> GetBusinessPartnersDirectory()
        {
            return HandleRequestAsync(_logger, "Error in GetBusinessPartnersDirectory endpoint", async () =>
            {
                var result = await _businessPartnerService.GetBusinessPartners(new BusinessPartnerFilter { PageNumber = 1, PageSize = 500 });
                return result.Items.Select(p => p.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<BusinessPartnerDto>> GetBusinessPartnerById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetBusinessPartnerById endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var partner = await _businessPartnerService.GetBusinessPartnerById(id, userId);
                if (partner == null)
                {
                    throw new KeyNotFoundException("Business partner not found");
                }

                return partner.ToDto();
            });
        }

        [HttpPost("search-business-partners")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<BusinessPartnerDto>>> SearchBusinessPartners([FromBody] BusinessPartnerFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search business partners endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _businessPartnerService.SearchBusinessPartners(filterDto.ToFilter(), userId);

                return new PagedResult<BusinessPartnerDto>
                {
                    Items = result.Items.Select(p => p.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

       
        [HttpPost]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<CreateBusinessPartnerResultDto>> CreateBusinessPartner(CreateBusinessPartnerDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateBusinessPartner endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var model = createDto.ToModel();
                await _businessPartnerService.CreateBusinessPartner(model, userId);
                return new CreateBusinessPartnerResultDto { Id = model.Id };
            });
        }

        [HttpPut("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateBusinessPartner(string id, CreateBusinessPartnerDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateBusinessPartner endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _businessPartnerService.UpdateBusinessPartner(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteBusinessPartner(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteBusinessPartner endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _businessPartnerService.DeleteBusinessPartner(id, userId);
            });
        }
    }
}
