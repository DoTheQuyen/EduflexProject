using ShareService.Models.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareService.DataAccess.Interface
{
    public interface IAuthentication
    {
        Task<UserModel?> FindByEmailAsync(string email);
        Task UpdateLastLoginAsync(string userId);
    }
}
