using System.ComponentModel;

namespace ShareService.Enums.Roles
{
    public enum SystemRole
    {
        [Description("Standard authenticated user")]
        Student = 1,

        [Description("Front-line staff with limited access")]
        Staff = 2,

        [Description("Manages finance and course promotions")]
        Manager = 3,

        [Description("Full administrative access")]
        Admin = 4
    }
}