using ShareService.Models.VisaProcess;

namespace ShareService.Mapping
{
    public static class VisaProcessTemplateMappingExtension
    {
        // Server-owned Id/Version are never touched here — Id is addressed by the route,
        // Version is bumped by VisaProcessTemplateService itself on every update, matching
        // every other ApplyEditableFields in this codebase.
        public static void ApplyEditableFields(this VisaProcessTemplateModel existing, VisaProcessTemplateModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Country = updateModel.Country;
            existing.Category = updateModel.Category;
            existing.Description = updateModel.Description;
            existing.Status = updateModel.Status;
            existing.IsDefaultForCountry = updateModel.IsDefaultForCountry;
            existing.Steps = updateModel.Steps;
        }
    }
}
