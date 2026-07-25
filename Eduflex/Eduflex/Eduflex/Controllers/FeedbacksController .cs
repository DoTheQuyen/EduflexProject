using Eduflex.DTOs.Feedback;
using Eduflex.Mapping.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.Models.Setting;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbacksController : BaseApiController
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbacksController> _logger;
        private readonly FeedbackSettings _feedbackSettings;

        public FeedbacksController(IFeedbackService feedbackService, ILogger<FeedbacksController> logger, IOptions<FeedbackSettings> feedbackSettings)
        {
            _feedbackService = feedbackService;
            _logger = logger;
            _feedbackSettings = feedbackSettings.Value;
        }

        [HttpGet("feedback-latest")]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<List<FeedbackDto>>> GetLatestFeedback([FromQuery] int? count = null)
        {
            return HandleRequestAsync(_logger, "Error in GetLatestFeedback endpoint", async () =>
            {
                var effectiveCount = count ?? _feedbackSettings.DefaultLatestCount;
                var feedbacks = await _feedbackService.GetLatestFeedback(effectiveCount);
                return feedbacks.Select(f => f.ToDto()).ToList();
            });
        }

        [HttpPost]
        [Authorize]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> CreateFeedback(CreateFeedbackDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateFeedback endpoint", () => _feedbackService.CreateFeedback(createDto.ToModel()));
        }

        [HttpPost("search-feedbacks")]
        [Authorize]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<FeedbackDto>>> SearchFeedbacks([FromBody] FeedbackFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search feedback endpoint", async () =>
            {
                var result = await _feedbackService.GetFeedback(filterDto.ToFilter());
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
        [Authorize]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteFeedback(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteFeedback endpoint", () =>
                _feedbackService.DeleteFeedback(id)
            );
        }
    }
}
