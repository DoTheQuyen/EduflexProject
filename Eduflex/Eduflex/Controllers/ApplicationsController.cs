// Controllers/ApplicationsController.cs
using Eduflex.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<ApplicationDto>>> GetStudentApplications(string studentId)
    {
        try
        {
            var applications = await _applicationService.GetApplicationsByStudentId(studentId);
            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for student {StudentId}", studentId);
            return StatusCode(500, "Error retrieving applications");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(string id)
    {
        try
        {
            var application = await _applicationService.GetApplicationById(id);
            if (application == null)
                return NotFound($"Application with ID {id} not found");

            return Ok(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application {ApplicationId}", id);
            return StatusCode(500, "Error retrieving application");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> CreateApplication([FromBody] CreateApplicationDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var application = await _applicationService.CreateApplication(createDto);
            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, "Error creating application");
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateApplicationStatus(string id, [FromBody] UpdateStatusDto statusDto)
    {
        try
        {
            var success = await _applicationService.UpdateApplicationStatus(id, statusDto.Status);
            if (!success)
                return NotFound($"Application with ID {id} not found");

            return Ok(new { message = "Application status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for application {ApplicationId}", id);
            return StatusCode(500, "Error updating application status");
        }
    }
}

// Models/UpdateStatusDto.cs
public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}