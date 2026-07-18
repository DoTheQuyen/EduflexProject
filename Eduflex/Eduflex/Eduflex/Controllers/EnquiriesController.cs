using Eduflex.API.DTOs;
using Eduflex.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiExplorerSettings(GroupName = "app")]
    public class EnquiriesController : ControllerBase
    {
        private readonly IEnquiryService _enquiryService;
        private readonly ILogger<EnquiriesController> _logger;

        public EnquiriesController(IEnquiryService enquiryService, ILogger<EnquiriesController> logger)
        {
            _enquiryService = enquiryService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<EnquiryDto>> CreateEnquiry(CreateEnquiryDto createDto)
        {
            try
            {
                var enquiry = await _enquiryService.CreateEnquiry(createDto.ToModel());
                return Ok(enquiry.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateEnquiry endpoint");
                return StatusCode(500, "An error occurred while submitting your enquiry");
            }
        }
    }
}
