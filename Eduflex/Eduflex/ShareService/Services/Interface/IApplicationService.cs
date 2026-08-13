using ShareService.Common;
using ShareService.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareService.Services.Interface
{
    public interface IApplicationService
    {
        Task<List<ApplicationModel>> GetApplicationsByStudentId(string studentId);
        Task<List<ApplicationModel>> GetApplicationsForStudentAsync(string studentId, string actingUserId);
        Task<ApplicationDetailModel?> GetApplicationDetailForStaffAsync(string id, string actingUserId);
        Task<PagedResult<ApplicationModel>> GetApplicationsByUserId(string userId, PaginationQuery query);
        Task<PagedResult<ApplicationModel>> GetAllApplicationsAsync(PaginationQuery query, string actingUserId);
        Task<ApplicationDetailModel?> GetApplicationById(string id, string userId);
        Task<ApplicationModel> CreateApplication(ApplicationModel application, string userId);
        Task<bool> UpdateApplicationStatus(string id, string status, string userId);
        Task<int> CountPendingApplicationsAsync(string userId);
    }
}
