using Eduflex.API.DTOs;
using Eduflex.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareService.Models.Setting;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class FeedbacksController : ControllerBase
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

        [HttpGet("latest")]
        [AllowAnonymous]
        public async Task<ActionResult<List<FeedbackDto>>> GetLatest([FromQuery] int? count = null)
        {
            var effectiveCount = count ?? _feedbackSettings.DefaultLatestCount;
            var feedbacks = await _feedbackService.GetLatestFeedback(effectiveCount);
            return Ok(feedbacks.Select(f => f.ToDto()).ToList());
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<FeedbackDto>> CreateFeedback(CreateFeedbackDto createDto)
        {
            try
            {
                var feedback = await _feedbackService.CreateFeedback(createDto.ToModel());
                return Ok(feedback.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFeedback endpoint");
                return StatusCode(500, "An error occurred while saving the feedback");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<FeedbackDto>>> GetAll()
        {
            var feedbacks = await _feedbackService.GetAllFeedback();
            return Ok(feedbacks.Select(f => f.ToDto()).ToList());
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFeedback(string id)
        {
            var deleted = await _feedbackService.DeleteFeedback(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}