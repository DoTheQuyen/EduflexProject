using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShareService.Enums.Roles
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnquiryEnums
    {
        [Description("New")]
        New = 1,

        [Description("Information request")]
        MIR = 2,

        [Description("Responded")]
        Responded = 3,

        [Description("Converted application")]
        Converted = 4,

    }
}