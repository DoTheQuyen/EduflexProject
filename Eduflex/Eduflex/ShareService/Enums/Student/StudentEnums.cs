using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShareService.Enums.Student
{
    // Student = 0 so legacy documents with no "type" field (every record created before
    // this enum existed) deserialize as Student without a data migration.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PersonType
    {
        [Description("Student")]
        Student = 0,

        [Description("Customer")]
        Customer = 1,
    }
}
