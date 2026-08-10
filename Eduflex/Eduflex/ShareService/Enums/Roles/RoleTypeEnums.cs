using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShareService.Enums.Roles
{
    // Numeric values double as hierarchy rank: lower number = more privileged.
    // UserService.CheckNotEscalatingRoleAsync compares these directly via
    // RoleTypeEnumsExtensions.IsHigherThan. If you insert a new type that
    // should rank between two existing ones, renumber the members below it
    // instead of appending — appending only ever adds the lowest rank.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleTypeEnums
    {
        [Description("Full administrative access")]
        Admin = 1,

        [Description("Manages finance and course promotions")]
        Manager = 2,

        [Description("Front-line staff with limited access")]
        Staff = 3,

        [Description("Standard authenticated student")]
        Student = 4,

        [Description("General customer (reserved for future visa module)")]
        Customer = 5,
    }

    public static class RoleTypeEnumsExtensions
    {
        public static bool IsHigherThan(this RoleTypeEnums role, RoleTypeEnums than) => (int)role < (int)than;
    }
}