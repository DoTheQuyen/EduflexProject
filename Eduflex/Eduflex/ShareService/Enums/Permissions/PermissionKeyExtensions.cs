using System.ComponentModel;
using System.Reflection;

namespace ShareService.Enums.Permissions
{
    public static class PermissionKeyExtensions
    {
        public static string GetKey(this PermissionKey permission)
        {
            var field = permission.GetType().GetField(permission.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? permission.ToString();
        }
    }
}
