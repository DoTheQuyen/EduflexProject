using Microsoft.AspNetCore.Authorization;
using ShareService.Enums;
using ShareService.Enums.Permissions;

namespace Eduflex.Authorization
{
    public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
    {
        public string Permission { get; }

        public RequirePermissionAttribute(PermissionKey permission)
        {
            Permission = permission.GetDescription();
        }

        public IEnumerable<IAuthorizationRequirement> GetRequirements()
        {
            yield return new PermissionRequirement(Permission);
        }
    }
}