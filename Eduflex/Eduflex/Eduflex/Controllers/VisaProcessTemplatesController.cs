using Eduflex.DTOs.VisaProcess;
using Eduflex.Mapping.VisaProcess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class VisaProcessTemplatesController : BaseApiController
    {
        private readonly IVisaProcessTemplateService _templateService;
        private readonly ILogger<VisaProcessTemplatesController> _logger;

        public VisaProcessTemplatesController(IVisaProcessTemplateService templateService, ILogger<VisaProcessTemplatesController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        [HttpGet]
        public Task<ActionResult<List<VisaProcessTemplateDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll VISA process templates endpoint", async () =>
            {
                var templates = await _templateService.GetAllAsync();
                return templates.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<VisaProcessTemplateDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById VISA process template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var template = await _templateService.GetByIdAsync(id, userId)
                    ?? throw new KeyNotFoundException("VISA process template not found");
                return template.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<VisaProcessTemplateDto>> Create(SaveVisaProcessTemplateDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create VISA process template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var created = await _templateService.CreateAsync(createDto.ToModel(), userId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> Update(string id, SaveVisaProcessTemplateDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in Update VISA process template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _templateService.UpdateAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpPost("{id}/status")]
        public Task<ActionResult<bool>> SetStatus(string id, SetVisaProcessTemplateStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetStatus VISA process template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _templateService.SetStatusAsync(id, statusDto.IsActive, userId);
            });
        }
    }
}
