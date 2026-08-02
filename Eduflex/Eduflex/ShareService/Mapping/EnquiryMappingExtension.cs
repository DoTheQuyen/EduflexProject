using ShareService.Models.Enquiry;

namespace ShareService.Mapping
{
    public static class EnquiryMappingExtension
    {
        public static void ApplyEditableFields(this EnquiryModel existing, EnquiryModel updateModel)
        {
            // Subject and CoursePromotionId are set once at creation and never editable by staff.
            existing.FirstName = updateModel.FirstName;
            existing.MiddleName = updateModel.MiddleName;
            existing.LastName = updateModel.LastName;
            existing.Email = updateModel.Email;
            existing.Mobile = updateModel.Mobile;
            existing.Enquiry = updateModel.Enquiry;
            existing.Status = updateModel.Status;
            existing.Response = updateModel.Response;
        }
    }
}
