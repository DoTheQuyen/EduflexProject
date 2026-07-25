using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduflex.API.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected string? GetActingUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        protected async Task<ActionResult<bool>> HandleCreateAsync<T>(ILogger logger, string errorContext, Func<Task<T>> action)
        {
            try
            {
                await action();
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorContext);
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        protected async Task<ActionResult<bool>> HandleUpdateAsync<T>(ILogger logger, string errorContext, Func<Task<T>> action)
        {
            try
            {
                await action();
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorContext);
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        protected async Task<ActionResult<T>> HandleRequestAsync<T>(ILogger logger, string errorContext, Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorContext);
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        protected async Task<IActionResult> HandleDeleteAsync(ILogger logger, string errorContext, Func<Task<bool>> action)
        {
            try
            {
                var deleted = await action();
                return deleted ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorContext);
                return StatusCode(500, "An unexpected error occurred");
            }
        }
    }
}