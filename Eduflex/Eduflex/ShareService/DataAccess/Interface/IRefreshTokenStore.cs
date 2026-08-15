using ShareService.Models.Auth;

namespace ShareService.DataAccess.Interface
{
    public interface IRefreshTokenStore
    {
        Task CreateAsync(RefreshTokenModel token);
        Task<RefreshTokenModel?> FindByHashAsync(string tokenHash);
        Task RevokeAsync(string id);
        Task RevokeAllForUserAsync(string userId);
    }
}