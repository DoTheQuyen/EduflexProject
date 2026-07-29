using Eduflex.DTOs.Settings;
using Eduflex.Mapping.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : BaseApiController
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // Any authenticated user can read effective settings — the student application
        // form needs this to enforce document-upload limits, not just staff/admin.
        [HttpGet]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<SettingsDto>> GetSettings()
        {
            return HandleRequestAsync(_logger, "Error in GetSettings endpoint", async () =>
            {
                var settings = await _settingsService.GetSettingsAsync();
                return settings.ToDto();
            });
        }

        [HttpPut]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateSettings(UpdateSettingsDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateSettings endpoint", () =>
            {
                var userId = GetRequiredUserId();
                return _settingsService.UpdateSettingsAsync(updateDto.ToModel(), userId);
            });
        }
    }
}
