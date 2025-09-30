using ShareService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareService.Services.Interface
{
    public interface IAuthService
    {
        Task<UserModel?> ValidateUserAsync(string email, string password, Func<string, string, bool> verifyPassword);
        Task UpdateLastLoginAsync(string userId);
    }
}
