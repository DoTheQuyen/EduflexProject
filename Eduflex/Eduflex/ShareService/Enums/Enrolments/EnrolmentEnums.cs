using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ShareService.Enums.Roles
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnrolmentEnums
    {
        [Description("Draft")]
        Draft = 1,

        [Description("Offer")]
        Offer = 2,

        [Description("COE")]
        Coe = 3,

        [Description("Apply VISA")]
        ApplyVisa = 4,

        [Description("VISA success")]
        VisaSuccess = 5,

        [Description("VISA fail")]
        VisaFail = 6,

        [Description("Cancel")]
        Cancel = 7,

        [Description("Completed")]
        Completed = 8,

        [Description("Finalized")]
        Finalized = 9,
    }
}
