using System.Text.Json.Serialization;

namespace ShareService.Enums.Permissions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionKey
    {
        ApplicationsView,
        ApplicationsAdd,
        ApplicationsEdit,
        ApplicationsDelete,

        FinanceView,
        FinanceAdd,
        FinanceEdit,
        FinanceDelete,

        CoursePromotionsView,
        CoursePromotionsAdd,
        CoursePromotionsEdit,
        CoursePromotionsDelete,

        RolesView,
        RolesAdd,
        RolesEdit,
        RolesDelete,

        UsersView,
        UsersAdd,
        UsersEdit,
        UsersDelete,

        EnquiryView,
        EnquiryAdd,
        EnquiryEdit,
        EnquiryDelete,

        EducationPartnersView,
        EducationPartnersAdd,
        EducationPartnersEdit,
        EducationPartnersDelete,

        BusinessPartnersView,
        BusinessPartnersAdd,
        BusinessPartnersEdit,
        BusinessPartnersDelete,

        EnrolmentsView,
        EnrolmentsAdd,
        EnrolmentsEdit,
        EnrolmentsDelete,
        EnrolmentsReassign,

        FeedbackView,
        FeedbackAdd,
        FeedbackEdit,
        FeedbackDelete,

        StudentsView,
        StudentsAdd,
        StudentsEdit,
        StudentsDelete,

        SettingsEdit,
    }
}