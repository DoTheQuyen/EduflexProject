using Eduflex.DTOs.Task;
using Eduflex.Mapping.Task;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Enums.Task;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    // Class/route named Tasks, not TaskItems — "TaskItem" is only a backend naming
    // device to dodge the System.Threading.Tasks.Task collision; the public API surface
    // and the frontend both just call this "Tasks".
    [ApiController]
    [Route("api/Tasks")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class TaskItemsController : BaseApiController
    {
        private readonly ITaskItemService _taskService;
        private readonly ILogger<TaskItemsController> _logger;

        public TaskItemsController(ITaskItemService taskService, ILogger<TaskItemsController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpPost("search-my-tasks")]
        public Task<ActionResult<PagedResult<TaskItemDto>>> SearchMyTasks([FromBody] TaskItemFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in SearchMyTasks endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _taskService.SearchMyTasksAsync(filterDto.ToFilter(), userId);
                return ToPagedDto(result);
            });
        }

        [HttpPost("search-all-tasks")]
        public Task<ActionResult<PagedResult<TaskItemDto>>> SearchAllTasks([FromBody] TaskItemFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in SearchAllTasks endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _taskService.SearchAllTasksAsync(filterDto.ToFilter(), userId);
                return ToPagedDto(result);
            });
        }

        [HttpPost("search-linked-tasks")]
        public Task<ActionResult<PagedResult<TaskItemDto>>> SearchLinkedTasks([FromBody] TaskItemFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in SearchLinkedTasks endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _taskService.SearchLinkedTasksAsync(filterDto.ToFilter(), userId);
                return ToPagedDto(result);
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<TaskItemDto>> GetTaskById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetTaskById endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var task = await _taskService.GetTaskByIdAsync(id, userId);
                if (task == null)
                {
                    throw new KeyNotFoundException("Task not found");
                }
                return task.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<CreateTaskItemResultDto>> CreateTask(CreateTaskItemDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateTask endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var model = createDto.ToModel();
                await _taskService.CreateTaskAsync(model, userId);
                return new CreateTaskItemResultDto { Id = model.Id };
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> UpdateTask(string id, UpdateTaskItemDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateTask endpoint", () =>
            {
                var userId = GetRequiredUserId();
                return _taskService.UpdateTaskAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpPost("{id}/notes")]
        public Task<ActionResult<TaskItemDto>> AddNote(string id, AddTaskNoteDto noteDto)
        {
            return HandleRequestAsync(_logger, "Error in AddNote endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var task = await _taskService.AddNoteAsync(id, noteDto.Content, userId);
                return task.ToDto();
            });
        }

        [HttpPut("{id}/reassign")]
        public Task<ActionResult<bool>> Reassign(string id, ReassignTaskItemDto reassignDto)
        {
            return HandleUpdateAsync(_logger, "Error in Reassign endpoint", () =>
            {
                var userId = GetRequiredUserId();
                return _taskService.ReassignTaskAsync(id, reassignDto.NewAssigneeUserId, reassignDto.Note, userId);
            });
        }

        [HttpPut("{id}/status")]
        public Task<ActionResult<bool>> ChangeStatus(string id, ChangeTaskItemStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in ChangeStatus endpoint", () =>
            {
                var userId = GetRequiredUserId();
                if (!Enum.TryParse<TaskItemStatus>(statusDto.Status, out var newStatus))
                {
                    throw new ArgumentException($"Unknown status '{statusDto.Status}'");
                }
                return _taskService.ChangeStatusAsync(id, newStatus, userId);
            });
        }

        private static PagedResult<TaskItemDto> ToPagedDto(PagedResult<ShareService.Models.Task.TaskItemModel> result)
        {
            return new PagedResult<TaskItemDto>
            {
                Items = result.Items.Select(t => t.ToDto()).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
