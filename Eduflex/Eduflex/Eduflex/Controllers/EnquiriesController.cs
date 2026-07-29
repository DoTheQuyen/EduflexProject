using Eduflex.Authorization;
using Eduflex.DTOs.Common;
using Eduflex.DTOs.Enquiry;
using Eduflex.Mapping.Enquiry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnquiriesController : BaseApiController
    {
        private readonly IEnquiryService _enquiryService;
        private readonly ILogger<EnquiriesController> _logger;
        private readonly IUserService _userService;

        public EnquiriesController(IEnquiryService enquiryService, IUserService userService, ILogger<EnquiriesController> logger)
        {
            _enquiryService = enquiryService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<Dictionary<string, string>> ResolveUserNamesAsync(IEnumerable<string?> userIds)
        {
            var distinctIds = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var result = new Dictionary<string, string>();
            foreach (var id in distinctIds)
            {
                var user = await _userService.GetUserByIdAsync(id!);
                if (user != null)
                {
                    result[id!] = $"{user.FirstName} {user.LastName}".Trim();
                }
            }
            return result;
        }

        [HttpPost]
        [AllowAnonymous]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<bool>> CreateEnquiry(CreateEnquiryDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateEnquiry endpoint", () => _enquiryService.CreateEnquiry(createDto.ToModel()));
        }

        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<EnquiryDto>> Get(string id)
        {
            return HandleRequestAsync(_logger, "Error in Get enquiry endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var enquiry = await _enquiryService.GetEnquiryAsync(id, userId)
                    ?? throw new KeyNotFoundException("Enquiry not found");
                var userNames = await ResolveUserNamesAsync(new[] { enquiry.UpdatedBy });
                return enquiry.ToDto(userNames);
            });
        }

        [HttpGet("enquiry-statuses")]
        [RequirePermission(PermissionKey.EnquiryView)]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<EnquiryStatusDto>>> GetEnquiryStatuses()
        {
            return HandleRequestAsync(_logger, "Error in GetEnquiryStatuses endpoint", () =>
                Task.FromResult(Enum.GetValues<EnquiryEnums>()
                    .Select(s => new EnquiryStatusDto { Value = s.ToString(), Label = s.GetDescription() })
                    .ToList())
            );
        }

        [HttpPost("search-enquiries")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<PagedResult<EnquiryDto>>> SearchEnquiries([FromBody] EnquiryFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search enquiries endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _enquiryService.GetEnquiries(filterDto.ToFilter(), userId);
                var userNames = await ResolveUserNamesAsync(result.Items.Select(e => e.UpdatedBy));
                return new PagedResult<EnquiryDto>
                {
                    Items = result.Items.Select(e => e.ToDto(userNames)).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPut("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<bool>> UpdateEnquiry(string id, EnquiryDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateEnquiry endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _enquiryService.UpdateEnquiriesAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<IActionResult> DeleteEnquiry(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteEnquiry endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _enquiryService.DeleteEnquiriesAsync(id, userId);
            });
        }

    }
}
