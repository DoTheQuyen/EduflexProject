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
    public class CoursePromotionsController : ControllerBase
    {
        private readonly ICoursePromotionService _coursePromotionService;
        private readonly ILogger<CoursePromotionsController> _logger;
        private readonly CoursePromotionSettings _coursePromotionSettings;

        public CoursePromotionsController(
            ICoursePromotionService coursePromotionService,
            ILogger<CoursePromotionsController> logger,
            IOptions<CoursePromotionSettings> coursePromotionSettings)
        {
            _coursePromotionService = coursePromotionService;
            _logger = logger;
            _coursePromotionSettings = coursePromotionSettings.Value;
        }

        [HttpGet("latest")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CoursePromotionDto>>> GetLatest([FromQuery] int? count = null)
        {
            var effectiveCount = count ?? _coursePromotionSettings.DefaultLatestCount;
            var promotions = await _coursePromotionService.GetFeaturedActiveCoursePromotions(effectiveCount);
            return Ok(promotions.Select(p => p.ToDto()).ToList());
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<CoursePromotionDto>>> GetAll()
        {
            var promotions = await _coursePromotionService.GetAllCoursePromotions();
            return Ok(promotions.Select(p => p.ToDto()).ToList());
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CoursePromotionDto>> CreateCoursePromotion(CreateCoursePromotionDto createDto)
        {
            try
            {
                var promotion = await _coursePromotionService.CreateCoursePromotion(createDto.ToModel());
                return Ok(promotion.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCoursePromotion endpoint");
                return StatusCode(500, "An error occurred while saving the course promotion");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<CoursePromotionDto>> UpdateCoursePromotion(string id, CreateCoursePromotionDto updateDto)
        {
            try
            {
                var promotion = await _coursePromotionService.UpdateCoursePromotion(id, updateDto.ToModel());
                if (promotion == null)
                {
                    return NotFound();
                }
                return Ok(promotion.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCoursePromotion endpoint");
                return StatusCode(500, "An error occurred while updating the course promotion");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCoursePromotion(string id)
        {
            var deleted = await _coursePromotionService.DeleteCoursePromotion(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
