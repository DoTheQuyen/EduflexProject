using Eduflex.DTOs.Department;
using Eduflex.Mapping.Department;
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
    public class DepartmentsController : BaseApiController
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        // Lightweight, non-permission-gated directory — used by pickers elsewhere (parent
        // department dropdown, staff-to-department assignment, the Users list department
        // badges) that need id/name lookups without requiring DepartmentsView. The gated
        // GetDepartmentById/SearchDepartments below are the staff-management screens.
        [HttpGet]
        public Task<ActionResult<List<DepartmentDto>>> GetDepartmentsDirectory()
        {
            return HandleRequestAsync(_logger, "Error in GetDepartmentsDirectory endpoint", async () =>
            {
                var departments = await _departmentService.GetAllDepartmentsAsync();
                return departments.Select(d => d.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<DepartmentDto>> GetDepartmentById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetDepartmentById endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var department = await _departmentService.GetDepartmentByIdAsync(id, userId);
                if (department == null)
                {
                    throw new KeyNotFoundException("Department not found");
                }

                return department.ToDto();
            });
        }

        [HttpPost("search-departments")]
        public Task<ActionResult<PagedResult<DepartmentDto>>> SearchDepartments([FromBody] DepartmentFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search departments endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _departmentService.SearchDepartmentsAsync(filterDto.ToFilter(), userId);
                return new PagedResult<DepartmentDto>
                {
                    Items = result.Items.Select(d => d.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPost]
        public Task<ActionResult<CreateDepartmentResultDto>> CreateDepartment(CreateDepartmentDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateDepartment endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var model = createDto.ToModel();
                await _departmentService.CreateDepartmentAsync(model, userId);
                return new CreateDepartmentResultDto { Id = model.Id };
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> UpdateDepartment(string id, CreateDepartmentDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateDepartment endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _departmentService.UpdateDepartmentAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteDepartment(string id)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteDepartment endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _departmentService.DeleteDepartmentAsync(id, userId);
            });
        }
    }
}
