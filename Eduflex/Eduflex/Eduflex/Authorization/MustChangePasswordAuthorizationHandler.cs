using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ShareService.Services.Interface;
using System.Security.Claims;

namespace Eduflex.Authorization
{
    public class MustChangePasswordAuthorizationHandler : AuthorizationHandler<MustChangePasswordRequirement>
    {
        private readonly IUserService _userService;

        public MustChangePasswordAuthorizationHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MustChangePasswordRequirement requirement)
        {
          
            var endpoint = context.Resource switch
            {
                HttpContext httpContext => httpContext.GetEndpoint(),
                Endpoint e => e,
                _ => null
            };

            if (endpoint?.Metadata.GetMetadata<SkipMustChangePasswordCheckAttribute>() != null)
            {
                context.Succeed(requirement);
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null || !user.MustChangePassword)
            {
                context.Succeed(requirement);
            }
        }
    }
}