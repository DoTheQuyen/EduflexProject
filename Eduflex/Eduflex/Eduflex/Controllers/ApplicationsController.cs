using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Models;
using System.Security.Claims;
using ShareService.Services;
using ShareService.Services.Interface;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ApiExplorerSettings(GroupName = "app")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(IApplicationService applicationService, ILogger<ApplicationsController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApplicationModel>>> GetApplications()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var applications = await _applicationService.GetApplicationsByUserId(userId);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetApplications endpoint");
            return StatusCode(500, "An error occurred while retrieving applications");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDetailModel>> GetApplication(string id)
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
    public async Task<ActionResult<ApplicationModel>> CreateApplication(CreateApplicationModel createDto)
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

            var application = await _applicationService.CreateApplication(createDto);
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
    [Authorize(Roles = "Admin")] // Only admin can update status
    public async Task<ActionResult> UpdateApplicationStatus(string id, [FromBody] string status)
    {
        try
        {
            var result = await _applicationService.UpdateApplicationStatus(id, status);
            if (!result)
            {
                return NotFound("Application not found");
            }

            return Ok("Application status updated successfully");
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

