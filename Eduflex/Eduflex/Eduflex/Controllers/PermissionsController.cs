using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Enums.Permissions;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        [HttpGet("permission-keys")]
        [Authorize]
        [ApiExplorerSettings(GroupName = "app")]
        public ActionResult<List<PermissionKey>> GetPermissionKeys()
        {
            return Ok(Enum.GetValues<PermissionKey>().ToList());
        }
    }
}