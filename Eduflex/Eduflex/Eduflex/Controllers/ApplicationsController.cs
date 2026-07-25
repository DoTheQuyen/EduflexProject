using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Models.Application;
using ShareService.Enums.Permissions;
using ShareService.Common;
using Eduflex.DTOs.Application;
using Eduflex.Mapping.Application;
using Eduflex.Authorization;
using Eduflex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(GroupName = "app")]
public class ApplicationsController : BaseApiController
{
    private readonly IApplicationService _applicationService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(IApplicationService applicationService, ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<ActionResult<PagedResult<ApplicationDto>>> GetApplications([FromQuery] PaginationQuery query)
    {
        return HandleRequestAsync(_logger, "Error in GetApplications endpoint", async () =>
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User ID not found in token");

            var result = await _applicationService.GetApplicationsByUserId(userId, query);
            return new PagedResult<ApplicationDto>
            {
                Items = result.Items.Select(a => a.ToDto()).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(string id)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var application = await _applicationService.GetApplicationById(id, userId);
            if (application == null)
            {
                return NotFound("Application not found");
            }

            return Ok(application);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to this application");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetApplication endpoint for ID: {ApplicationId}", id);
            return StatusCode(500, "An error occurred while retrieving application details");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationModel>> CreateApplication(CreateApplicationDto createDto)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Set userId from token, not from request body
            createDto.UserId = userId;

            var application = await _applicationService.CreateApplication(createDto.ToModel());
            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateApplication endpoint");
            return StatusCode(500, "An error occurred while creating application");
        }
    }

    [HttpPut("{id}/status")]
    [RequirePermission(PermissionKey.ApplicationsEdit)]
    public async Task<ActionResult> UpdateApplicationStatus(string id, [FromBody] string status)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var result = await _applicationService.UpdateApplicationStatus(id, status, userId);
            if (!result)
            {
                return NotFound("Application not found");
            }

            return Ok("Application status updated successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to update application status");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateApplicationStatus endpoint for ID: {ApplicationId}", id);
            return StatusCode(500, "An error occurred while updating application status");
        }
    }

    private string GetUserIdFromToken()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? string.Empty;
    }

}
