using Eduflex.DTOs.EducationPartner;
using Eduflex.Mapping.EducationPartner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Models.EducationPartner;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EducationPartnersController : BaseApiController
    {
        private readonly IEducationPartnerService _educationPartnerService;
        private readonly ICourseService _courseService;
        private readonly IBusinessPartnerService _businessPartnerService;
        private readonly ILogger<EducationPartnersController> _logger;

        public EducationPartnersController(
            IEducationPartnerService educationPartnerService,
            ICourseService courseService,
            IBusinessPartnerService businessPartnerService,
            ILogger<EducationPartnersController> logger)
        {
            _educationPartnerService = educationPartnerService;
            _courseService = courseService;
            _businessPartnerService = businessPartnerService;
            _logger = logger;
        }

       
        [HttpGet("all-education-partners")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<EducationPartnerDirectoryDto>>> GetEducationPartnersDirectory()
        {
            return HandleRequestAsync(_logger, "Error in GetEducationPartnersDirectory endpoint", async () =>
            {
                var result = await _educationPartnerService.GetEducationPartners(new EducationPartnerFilter { PageNumber = 1, PageSize = 500 });
                var partnerIds = result.Items.Select(p => p.Id).ToList();
                var coursesByPartner = await _courseService.GetCoursesByPartnerIds(partnerIds);

                return result.Items.Select(p => p.ToDirectoryDto(
                    (coursesByPartner.TryGetValue(p.Id, out var courses) ? courses : new List<ShareService.Models.Course.CourseModel>())
                        .Select(c => c.ToDto()).ToList()
                )).ToList();
            });
        }

        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<EducationPartnerDto>> GetEducationPartnerById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetEducationPartnerById endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var partner = await _educationPartnerService.GetEducationPartnerById(id, userId);
                if (partner == null)
                {
                    throw new KeyNotFoundException("Education partner not found");
                }

                var courses = await _courseService.GetCoursesByPartnerId(id, userId);
                var businessPartnerName = await ResolveBusinessPartnerName(partner.BusinessPartnerId);
                return partner.ToDto(courses.Select(c => c.ToDto()).ToList(), businessPartnerName);
            });
        }

        [HttpPost("search-education-partners")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<EducationPartnerDto>>> SearchEducationPartners([FromBody] EducationPartnerFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search education partners endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _educationPartnerService.SearchEducationPartners(filterDto.ToFilter(), userId);
                var partnerIds = result.Items.Select(p => p.Id).ToList();
                var coursesByPartner = await _courseService.GetCoursesByPartnerIds(partnerIds);

                var businessPartnerIds = result.Items.Select(p => p.BusinessPartnerId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct();
                var businessPartners = await _businessPartnerService.GetBusinessPartnersByIds(businessPartnerIds);
                var businessPartnerNameById = businessPartners.ToDictionary(bp => bp.Id, bp => bp.Name);

                return new PagedResult<EducationPartnerDto>
                {
                    Items = result.Items.Select(p => p.ToDto(
                        (coursesByPartner.TryGetValue(p.Id, out var courses) ? courses : new List<ShareService.Models.Course.CourseModel>())
                            .Select(c => c.ToDto()).ToList(),
                        p.BusinessPartnerId != null && businessPartnerNameById.TryGetValue(p.BusinessPartnerId, out var name) ? name : null
                    )).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        private async Task<string?> ResolveBusinessPartnerName(string? businessPartnerId)
        {
            if (string.IsNullOrEmpty(businessPartnerId))
            {
                return null;
            }

            var matches = await _businessPartnerService.GetBusinessPartnersByIds(new[] { businessPartnerId });
            return matches.FirstOrDefault()?.Name;
        }

      
        [HttpPost]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<CreateEducationPartnerResultDto>> CreateEducationPartner(CreateEducationPartnerDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateEducationPartner endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var model = createDto.ToModel();
                await _educationPartnerService.CreateEducationPartner(model, userId);
                return new CreateEducationPartnerResultDto { Id = model.Id };
            });
        }

        [HttpPut("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateEducationPartner(string id, CreateEducationPartnerDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateEducationPartner endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _educationPartnerService.UpdateEducationPartner(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteEducationPartner(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteEducationPartner endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _educationPartnerService.DeleteEducationPartner(id, userId);
            });
        }
    }
}
