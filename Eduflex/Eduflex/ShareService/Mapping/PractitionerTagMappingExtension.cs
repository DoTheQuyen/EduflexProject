using ShareService.Models.VisaProcess;

namespace ShareService.Mapping
{
    public static class PractitionerTagMappingExtension
    {
        // Server-owned Id is never touched here — addressed by the route, matching every
        // other ApplyEditableFields in this codebase. Active is included here (editable via
        // the regular update call) same as DynamicFormTemplateMappingExtension includes
        // Status — SetActiveAsync is just a convenience toggle for the same field.
        public static void ApplyEditableFields(this PractitionerTagModel existing, PractitionerTagModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.ColorHex = updateModel.ColorHex;
            existing.Description = updateModel.Description;
            existing.Active = updateModel.Active;
        }
    }
}
