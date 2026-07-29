using Eduflex.DTOs.Enrolment;
using Eduflex.Mapping.Enrolment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class EmailTemplatesController : BaseApiController
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<EmailTemplatesController> _logger;

        public EmailTemplatesController(IEmailTemplateService emailTemplateService, ILogger<EmailTemplatesController> logger)
        {
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        [HttpGet]
        public Task<ActionResult<List<EmailTemplateDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll email templates endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var templates = await _emailTemplateService.GetAllAsync(userId);
                return templates.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpPost]
        public Task<ActionResult<EmailTemplateDto>> Create(CreateEmailTemplateDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create email template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var created = await _emailTemplateService.CreateAsync(createDto.ToModel(), userId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> Update(string id, UpdateEmailTemplateDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in Update email template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _emailTemplateService.UpdateAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(string id)
        {
            return HandleDeleteAsync(_logger, "Error in Delete email template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _emailTemplateService.DeleteAsync(id, userId);
            });
        }
    }
}
