using Eduflex.DTOs.Feedback;
using Eduflex.Mapping.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbacksController : BaseApiController
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbacksController> _logger;
        private readonly ISettingsService _settingsService;

        public FeedbacksController(IFeedbackService feedbackService, ILogger<FeedbacksController> logger, ISettingsService settingsService)
        {
            _feedbackService = feedbackService;
            _logger = logger;
            _settingsService = settingsService;
        }

        [HttpGet("feedback-latest")]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<List<FeedbackDto>>> GetLatestFeedback([FromQuery] int? count = null)
        {
            return HandleRequestAsync(_logger, "Error in GetLatestFeedback endpoint", async () =>
            {
                var settings = await _settingsService.GetSettingsAsync();
                var effectiveCount = count ?? settings.FeedbackDefaultLatestCount;
                var feedbacks = await _feedbackService.GetLatestFeedback(effectiveCount);
                return feedbacks.Select(f => f.ToDto()).ToList();
            });
        }

        [HttpPost]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> CreateFeedback(CreateFeedbackDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateFeedback endpoint", () => _feedbackService.CreateFeedback(createDto.ToModel()));
        }

        [HttpPost("search-feedbacks")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<FeedbackDto>>> SearchFeedbacks([FromBody] FeedbackFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search feedback endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _feedbackService.GetFeedback(filterDto.ToFilter(), userId);
                return new PagedResult<FeedbackDto>
                {
                    Items = result.Items.Select(f => f.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteFeedback(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteFeedback endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _feedbackService.DeleteFeedback(id, userId);
            });
        }
    }
}
