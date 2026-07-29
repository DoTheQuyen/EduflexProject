using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduflex.API.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected string? GetActingUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Every action on a [Authorize]-protected controller needs the caller's id as a
        // non-nullable string to hand to a service. AuthController always puts
        // ClaimTypes.NameIdentifier on every token it issues, so in practice this can't
        // actually be missing once [Authorize] has passed — but GetActingUserId() is
        // still typed string? (the compiler has no way to know that), so something has
        // to narrow it. This does that once, here, instead of every action repeating
        // the same three-line null-check-and-throw. Throwing (401) rather than using the
        // null-forgiving operator (!) means a future auth change that broke the
        // guarantee would fail loudly at the boundary, not crash somewhere downstream.
        protected string GetRequiredUserId() =>
            GetActingUserId() ?? throw new UnauthorizedAccessException("User not authenticated");

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