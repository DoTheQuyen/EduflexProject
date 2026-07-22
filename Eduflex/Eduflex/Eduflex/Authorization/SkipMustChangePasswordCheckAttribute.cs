namespace Eduflex.Authorization
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipMustChangePasswordCheckAttribute : Attribute
    {
    }
}
