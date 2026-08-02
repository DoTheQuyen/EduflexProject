using ShareService.Models.Enrolment;

namespace ShareService.Mapping
{
    public static class EmailTemplateMappingExtension
    {
        public static void ApplyEditableFields(this EmailTemplateModel existing, EmailTemplateModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Subject = updateModel.Subject;
            existing.Body = updateModel.Body;
        }
    }
}
