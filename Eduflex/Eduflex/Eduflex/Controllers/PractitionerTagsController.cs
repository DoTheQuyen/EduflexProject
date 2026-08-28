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
    public class PractitionerTagsController : BaseApiController
    {
        private readonly IPractitionerTagService _tagService;
        private readonly ILogger<PractitionerTagsController> _logger;

        public PractitionerTagsController(IPractitionerTagService tagService, ILogger<PractitionerTagsController> logger)
        {
            _tagService = tagService;
            _logger = logger;
        }

        [HttpGet("all-practitioner-tags")]
        public Task<ActionResult<List<PractitionerTagDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll practitioner tags endpoint", async () =>
            {
                var tags = await _tagService.GetAllAsync();
                return tags.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpPost]
        public Task<ActionResult<PractitionerTagDto>> Create(SavePractitionerTagDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create practitioner tag endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var created = await _tagService.CreateAsync(createDto.ToModel(), userId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> Update(string id, SavePractitionerTagDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in Update practitioner tag endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _tagService.UpdateAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpPost("{id}/status")]
        public Task<ActionResult<bool>> SetActive(string id, SetPractitionerTagActiveDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetActive practitioner tag endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _tagService.SetActiveAsync(id, statusDto.IsActive, userId);
            });
        }
    }
}
