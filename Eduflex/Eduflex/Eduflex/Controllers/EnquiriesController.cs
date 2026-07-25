using Eduflex.Authorization;
using Eduflex.DTOs.Enquiry;
using Eduflex.Mapping.Enquiry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[AllowAnonymous]
    //[ApiExplorerSettings(GroupName = "public")]
    public class EnquiriesController : BaseApiController
    {
        private readonly IEnquiryService _enquiryService;
        private readonly ILogger<EnquiriesController> _logger;

        public EnquiriesController(IEnquiryService enquiryService, ILogger<EnquiriesController> logger)
        {
            _enquiryService = enquiryService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<bool>> CreateEnquiry(CreateEnquiryDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateEnquiry endpoint", () => _enquiryService.CreateEnquiry(createDto.ToModel()));
        }

        [HttpGet("{id}")]
        [RequirePermission(PermissionKey.EnquiryView)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<EnquiryDto>> Get(string id)
        {
            return HandleRequestAsync(_logger, "Error in Get enquiry endpoint", async () =>
            {
                var enquiry = await _enquiryService.GetEnquiryAsync(id) 
                ?? throw new KeyNotFoundException("Enquiry not found");
                return enquiry.ToDto();
            });
        }

        [HttpGet]
        [RequirePermission(PermissionKey.EnquiryView)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<EnquiryDto>>> GetAll(EnquiryEnums status)
        {
            return HandleRequestAsync(_logger, "Error in GetAll enquiries endpoint", async () =>
            {
                var enquiries = await _enquiryService.GetAllEnquiriesAsync(status);
                return enquiries.Select(p => p.ToDto()).ToList();
            });
        }
      
        [HttpPut("{id}")]
        [RequirePermission(PermissionKey.EnquiryEdit)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateEnquiry(string id, EnquiryDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateEnquiry endpoint", () => _enquiryService.UpdateEnquiriesAsync(id, updateDto.ToModel()));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionKey.EnquiryDelete)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteEnquiry(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteEnquiry endpoint", () => 
                _enquiryService.DeleteEnquiriesAsync(id)
            );
        }
    }
}
