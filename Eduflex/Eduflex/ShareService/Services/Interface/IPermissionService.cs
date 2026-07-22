namespace ShareService.Services.Interface
{
    public interface IPermissionService
    {
        Task<List<string>> GetPermissionsForUserAsync(string userId);
    }
}