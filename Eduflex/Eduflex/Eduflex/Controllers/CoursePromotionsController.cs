using Eduflex.DTOs.Course;
using Eduflex.Mapping.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursePromotionsController : BaseApiController
    {
        private readonly ICoursePromotionService _coursePromotionService;
        private readonly ILogger<CoursePromotionsController> _logger;
        private readonly ISettingsService _settingsService;

        public CoursePromotionsController(
            ICoursePromotionService coursePromotionService,
            ILogger<CoursePromotionsController> logger,
            ISettingsService settingsService)
        {
            _coursePromotionService = coursePromotionService;
            _logger = logger;
            _settingsService = settingsService;
        }

        [HttpGet("course-latest")]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<List<CoursePromotionDto>>> GetFeaturedActiveCoursePromotions([FromQuery] int? count = null)
        {
            return HandleRequestAsync(_logger, "Error in GetFeaturedActiveCoursePromotions endpoint", async () =>
            {
                var settings = await _settingsService.GetSettingsAsync();
                var effectiveCount = count ?? settings.CoursePromotionDefaultLatestCount;
                var promotions = await _coursePromotionService.GetFeaturedActiveCoursePromotions(effectiveCount);
                return promotions.Select(p => p.ToDto()).ToList();
            });
        }

        [HttpPost("search-course-promotions")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<CoursePromotionDto>>> SearchCoursePromotions([FromBody] CoursePromotionFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search course promotions endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _coursePromotionService.GetCoursePromotions(filterDto.ToFilter(), userId);
                return new PagedResult<CoursePromotionDto>
                {
                    Items = result.Items.Select(p => p.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPost]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> CreateCoursePromotion(CreateCoursePromotionDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateCoursePromotion endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _coursePromotionService.CreateCoursePromotion(createDto.ToModel(), userId);
            });
        }

        [HttpPut("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateCoursePromotion(string id, CreateCoursePromotionDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateCoursePromotion endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _coursePromotionService.UpdateCoursePromotion(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteCoursePromotion(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteCoursePromotion endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _coursePromotionService.DeleteCoursePromotion(id, userId);
            });
        }
    }
}
