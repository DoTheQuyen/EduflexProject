using ShareService.Models.DynamicForm;

namespace ShareService.Mapping
{
    public static class DynamicFormTemplateMappingExtension
    {
        // Server-owned Id is never touched here — callers address templates by id on
        // the route, matching every other ApplyEditableFields in this codebase.
        public static void ApplyEditableFields(this DynamicFormTemplateModel existing, DynamicFormTemplateModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Description = updateModel.Description;
            existing.Status = updateModel.Status;
            existing.BoundStepKey = updateModel.BoundStepKey;
            existing.Questions = updateModel.Questions;
        }
    }
}
