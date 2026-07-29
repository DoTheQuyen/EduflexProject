using Eduflex.DTOs.EducationPartner;
using Eduflex.Mapping.EducationPartner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : BaseApiController
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ICourseService courseService, ILogger<CoursesController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        [HttpPost("search-courses")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<CourseSearchResultDto>>> SearchCourses([FromBody] CourseSearchFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search courses endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _courseService.SearchCourses(filterDto.ToFilter(), userId);
                return new PagedResult<CourseSearchResultDto>
                {
                    Items = result.Items.Select(x => x.ToSearchResultDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpGet("by-partner/{partnerId}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<CourseDto>>> GetCoursesByPartner(string partnerId)
        {
            return HandleRequestAsync(_logger, "Error in GetCoursesByPartner endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var courses = await _courseService.GetCoursesByPartnerId(partnerId, userId);
                return courses.Select(c => c.ToDto()).ToList();
            });
        }

        [HttpPost]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> CreateCourse(CreateCourseDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateCourse endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _courseService.CreateCourse(createDto.ToModel(), userId);
            });
        }

        [HttpPut("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateCourse(string id, CreateCourseDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateCourse endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _courseService.UpdateCourse(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteCourse(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteCourse endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _courseService.DeleteCourse(id, userId);
            });
        }
    }
}
