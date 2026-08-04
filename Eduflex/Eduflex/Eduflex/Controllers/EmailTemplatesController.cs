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

        // Lightweight, non-permission-gated directory — any authenticated staff member
        // needs this to populate a template picker when composing an enrolment/finance
        // email, same reasoning as DynamicFormTemplatesController.GetAll. The gated
        // GetById/Create/Update/SetStatus below are the admin management screen.
        [HttpGet]
        public Task<ActionResult<List<EmailTemplateDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll email templates endpoint", async () =>
            {
                var templates = await _emailTemplateService.GetAllAsync();
                return templates.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<EmailTemplateDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById email template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var template = await _emailTemplateService.GetByIdAsync(id, userId)
                    ?? throw new KeyNotFoundException("Template not found");
                return template.ToDto();
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

        [HttpPost("{id}/status")]
        public Task<ActionResult<bool>> SetStatus(string id, SetEmailTemplateStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetStatus email template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _emailTemplateService.SetStatusAsync(id, statusDto.IsActive, userId);
            });
        }
    }
}
