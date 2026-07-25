using System.ComponentModel;

namespace ShareService.Enums.Roles
{
    public enum EnquiryEnums
    {
        [Description("New enquiry")]
        New = 1,

        [Description("More information request enquiry")]
        MIR = 2,

        [Description("Responded enquiry")]
        Responded = 3,

        [Description("Converted to application")]
        Converted = 4,

    }
}