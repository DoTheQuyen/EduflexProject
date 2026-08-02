using ShareService.Models.DynamicForm;

namespace ShareService.DataAccess.Interface
{
    public interface IDynamicFormTemplate
    {
        Task<List<DynamicFormTemplateModel>> GetAllAsync();
        Task<DynamicFormTemplateModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(DynamicFormTemplateModel template);
        Task<bool> ReplaceAsync(string id, DynamicFormTemplateModel template);
    }
}
