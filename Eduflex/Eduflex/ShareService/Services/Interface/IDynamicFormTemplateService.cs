using ShareService.Models.DynamicForm;

namespace ShareService.Services.Interface
{
    public interface IDynamicFormTemplateService
    {
        // Lightweight directory, no permission gate — any authenticated staff member
        // needs this to know what's requestable from an Enrolment's Forms tab / VISA
        // step, same reasoning as DepartmentsController.GetDepartmentsDirectory.
        Task<List<DynamicFormTemplateModel>> GetAllAsync();

        Task<DynamicFormTemplateModel?> GetByIdAsync(string id, string userId);
        Task<DynamicFormTemplateModel> CreateAsync(DynamicFormTemplateModel template, string userId);
        Task<bool> UpdateAsync(string id, DynamicFormTemplateModel template, string userId);
        Task<bool> SetStatusAsync(string id, bool isActive, string userId);
    }
}
