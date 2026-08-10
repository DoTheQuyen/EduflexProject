using Eduflex.DTOs.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShareService.Models.Chat;
using ShareService.Services.Interface.Integration;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : BaseApiController
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("ask")]
        [AllowAnonymous]
        [EnableRateLimiting("ChatPolicy")]
        [ApiExplorerSettings(GroupName = "public")]
        public Task<ActionResult<ChatAnswerDto>> AskQuestion(AskQuestionDto askDto)
        {
            return HandleRequestAsync(_logger, "Error in AskQuestion endpoint", async () =>
            {
                var answer = await _chatService.AskQuestionAsync(askDto.Question);
                return new ChatAnswerDto { Answer = answer };
            });
        }

        [HttpGet("top-questions")]
        [ApiExplorerSettings(GroupName = "app")]
        public Task<ActionResult<List<QuestionFrequency>>> GetTopQuestions([FromQuery] int limit = 10)
        {
            return HandleRequestAsync(_logger, "Error in GetTopQuestions endpoint", () =>
                _chatService.GetTopQuestionsAsync(limit));
        }
    }
}