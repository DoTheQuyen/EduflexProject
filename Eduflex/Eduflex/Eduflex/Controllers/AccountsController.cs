using Eduflex.DTOs.Accounts;
using Eduflex.Mapping.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class AccountsController : BaseApiController
    {
        private readonly IAccountsService _accountsService;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAccountsService accountsService, ILogger<AccountsController> logger)
        {
            _accountsService = accountsService;
            _logger = logger;
        }

        // The Action Queue's whole point is staying small regardless of portfolio size —
        // windowDays bounds it to "overdue, or due within N days" rather than everything.
        [HttpGet("action-queue")]
        public Task<ActionResult<ActionQueueResultDto>> GetActionQueue([FromQuery] int windowDays = 14)
        {
            return HandleRequestAsync(_logger, "Error in GetActionQueue endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _accountsService.GetActionQueueAsync(windowDays, userId);
                return result.ToDto();
            });
        }

        [HttpGet]
        public Task<ActionResult<PagedResult<AccountSummaryDto>>> GetAccounts(
            [FromQuery] string? search, [FromQuery] string? accountType, [FromQuery] string? status,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            return HandleRequestAsync(_logger, "Error in GetAccounts endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _accountsService.GetPortfolioAsync(search, accountType, status, pageNumber, pageSize, userId);
                return new PagedResult<AccountSummaryDto>
                {
                    Items = result.Items.Select(a => a.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpGet("timeline")]
        public Task<ActionResult<AccountTimelineDto>> GetTimeline([FromQuery] string accountType, [FromQuery] string accountKey)
        {
            return HandleRequestAsync(_logger, "Error in GetTimeline endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _accountsService.GetAccountTimelineAsync(accountType, accountKey, userId);
                return result.ToDto();
            });
        }
    }
}
