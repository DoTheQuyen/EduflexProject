using Eduflex.DTOs.DynamicForm;
using Eduflex.Mapping.DynamicForm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class DynamicFormTemplatesController : BaseApiController
    {
        private readonly IDynamicFormTemplateService _templateService;
        private readonly ILogger<DynamicFormTemplatesController> _logger;

        public DynamicFormTemplatesController(IDynamicFormTemplateService templateService, ILogger<DynamicFormTemplatesController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

       
        [HttpGet]
        public Task<ActionResult<List<DynamicFormTemplateDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll dynamic form templates endpoint", async () =>
            {
                var templates = await _templateService.GetAllAsync();
                return templates.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<DynamicFormTemplateDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById dynamic form template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var template = await _templateService.GetByIdAsync(id, userId)
                    ?? throw new KeyNotFoundException("Form template not found");
                return template.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<DynamicFormTemplateDto>> Create(SaveDynamicFormTemplateDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create dynamic form template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var created = await _templateService.CreateAsync(createDto.ToModel(), userId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> Update(string id, SaveDynamicFormTemplateDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in Update dynamic form template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _templateService.UpdateAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpPost("{id}/status")]
        public Task<ActionResult<bool>> SetStatus(string id, SetDynamicFormTemplateStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetStatus dynamic form template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _templateService.SetStatusAsync(id, statusDto.IsActive, userId);
            });
        }
    }
}
