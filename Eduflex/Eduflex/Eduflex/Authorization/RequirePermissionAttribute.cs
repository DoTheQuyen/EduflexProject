using Microsoft.AspNetCore.Authorization;

namespace Eduflex.Authorization
{
    public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
    {
        public string Permission { get; }

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }

        public IEnumerable<IAuthorizationRequirement> GetRequirements()
        {
            yield return new PermissionRequirement(Permission);
        }
    }
}