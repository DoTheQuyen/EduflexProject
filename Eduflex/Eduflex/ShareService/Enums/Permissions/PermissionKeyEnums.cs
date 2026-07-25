using System.ComponentModel;

namespace ShareService.Enums.Permissions
{
    public enum PermissionKey
    {
        [Description("applications.view")]
        ApplicationsView,

        [Description("applications.add")]
        ApplicationsAdd,

        [Description("applications.edit")]
        ApplicationsEdit,

        [Description("applications.delete")]
        ApplicationsDelete,

        [Description("finance.view")]
        FinanceView,

        [Description("finance.add")]
        FinanceAdd,

        [Description("finance.edit")]
        FinanceEdit,

        [Description("finance.delete")]
        FinanceDelete,

        [Description("coursepromotions.view")]
        CoursePromotionsView,

        [Description("coursepromotions.add")]
        CoursePromotionsAdd,

        [Description("coursepromotions.edit")]
        CoursePromotionsEdit,

        [Description("coursepromotions.delete")]
        CoursePromotionsDelete,

        [Description("roles.view")]
        RolesView,

        [Description("roles.add")]
        RolesAdd,

        [Description("roles.edit")]
        RolesEdit,

        [Description("roles.delete")]
        RolesDelete,

        [Description("users.view")]
        UsersView,

        [Description("users.add")]
        UsersAdd,

        [Description("users.edit")]
        UsersEdit,

        [Description("users.delete")]
        UsersDelete,

        [Description("enquiry.view")]
        EnquiryView,

        [Description("enquiry.add")]
        EnquiryAdd,

        [Description("enquiry.edit")]
        EnquiryEdit,

        [Description("enquiry.delete")]
        EnquiryDelete,
    }
}
