using Eduflex.Authorization;
using Eduflex.DTOs.Course;
using Eduflex.Mapping.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.Enums.Permissions;
using ShareService.Models.Setting;
using ShareService.Services;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursePromotionsController : BaseApiController
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

        [HttpGet("course-latest")]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<List<CoursePromotionDto>>> GetFeaturedActiveCoursePromotions([FromQuery] int? count = null)
        {
            return HandleRequestAsync(_logger, "Error in GetFeaturedActiveCoursePromotions endpoint", async () =>
            {
                var effectiveCount = count ?? _coursePromotionSettings.DefaultLatestCount;
                var promotions = await _coursePromotionService.GetFeaturedActiveCoursePromotions(effectiveCount);
                return promotions.Select(p => p.ToDto()).ToList();
            });
        }

        [HttpPost("search-course-promotions")]
        [RequirePermission(PermissionKey.CoursePromotionsView)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<CoursePromotionDto>>> SearchCoursePromotions([FromBody] CoursePromotionFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search course promotions endpoint", async () =>
            {
                var result = await _coursePromotionService.GetCoursePromotions(filterDto.ToFilter());
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
        [RequirePermission(PermissionKey.CoursePromotionsAdd)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> CreateCoursePromotion(CreateCoursePromotionDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateCoursePromotion endpoint", () => _coursePromotionService.CreateCoursePromotion(createDto.ToModel()));
        }

        [HttpPut("{id}")]
        [RequirePermission(PermissionKey.CoursePromotionsEdit)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateCoursePromotion(string id, CreateCoursePromotionDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateCoursePromotion endpoint", () => _coursePromotionService.UpdateCoursePromotion(id, updateDto.ToModel()));
        }

        [HttpDelete("{id}")]
        [RequirePermission(PermissionKey.CoursePromotionsDelete)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteCoursePromotion(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteCoursePromotion endpoint", () =>
                _coursePromotionService.DeleteCoursePromotion(id)
            );
        }
    }
}
