using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ShareService.Services;
using ShareService.Services.Interface;
using ShareService.Models.Application;
using ShareService.Common;
using Eduflex.DTOs.Application;
using Eduflex.Mapping.Application;
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

    [HttpGet("my-applications")]
    public Task<ActionResult<PagedResult<ApplicationDto>>> GetApplications([FromQuery] PaginationQuery query)
    {
        return HandleRequestAsync(_logger, "Error in GetApplications endpoint", async () =>
        {
            var userId = GetRequiredUserId();

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

    [HttpGet("all-applications")]
    public Task<ActionResult<PagedResult<ApplicationDto>>> GetAllApplications([FromQuery] PaginationQuery query)
    {
        return HandleRequestAsync(_logger, "Error in GetAllApplications endpoint", async () =>
        {
            var actingUserId = GetRequiredUserId();

            var result = await _applicationService.GetAllApplicationsAsync(query, actingUserId);
            return new PagedResult<ApplicationDto>
            {
                Items = result.Items.Select(a => a.ToDto()).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        });
    }

    [HttpGet("by-student/{studentId}")]
    public Task<ActionResult<List<ApplicationDto>>> GetApplicationsForStudent(string studentId)
    {
        return HandleRequestAsync(_logger, $"Error getting applications for student: {studentId}", async () =>
        {
            var actingUserId = GetRequiredUserId();

            var applications = await _applicationService.GetApplicationsForStudentAsync(studentId, actingUserId);
            return applications.Select(a => a.ToDto()).ToList();
        });
    }

    [HttpGet("{id}/staff-view")]
    public Task<ActionResult<ApplicationDetailDto>> GetApplicationForStaff(string id)
    {
        return HandleRequestAsync(_logger, $"Error in GetApplicationForStaff endpoint for ID: {id}", async () =>
        {
            var actingUserId = GetRequiredUserId();

            var application = await _applicationService.GetApplicationDetailForStaffAsync(id, actingUserId)
                ?? throw new KeyNotFoundException("Application not found");

            return application.ToDto();
        });
    }

    [HttpGet("{id}")]
    public Task<ActionResult<ApplicationDetailDto>> GetApplication(string id)
    {
        return HandleRequestAsync(_logger, $"Error in GetApplication endpoint for ID: {id}", async () =>
        {
            var userId = GetRequiredUserId();

            var application = await _applicationService.GetApplicationById(id, userId)
                ?? throw new KeyNotFoundException("Application not found");

            return application.ToDto();
        });
    }

    [HttpPost]
    public Task<ActionResult<ApplicationModel>> CreateApplication(CreateApplicationDto createDto)
    {
        return HandleRequestAsync(_logger, "Error in CreateApplication endpoint", async () =>
        {
            var userId = GetRequiredUserId();

            // Set userId from token, not from request body
            createDto.UserId = userId;

            return await _applicationService.CreateApplication(createDto.ToModel(), userId);
        });
    }

    [HttpPut("{id}/status")]
    public Task<ActionResult<bool>> UpdateApplicationStatus(string id, [FromBody] string status)
    {
        return HandleUpdateAsync(_logger, $"Error in UpdateApplicationStatus endpoint for ID: {id}", async () =>
        {
            var userId = GetRequiredUserId();

            return await _applicationService.UpdateApplicationStatus(id, status, userId);
        });
    }

}
