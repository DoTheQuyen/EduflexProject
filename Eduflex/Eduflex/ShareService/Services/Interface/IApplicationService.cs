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
        Task<List<ApplicationModel>> GetApplicationsByUserId(string userId);
        Task<ApplicationDetailModel?> GetApplicationById(string id, string userId);
        Task<ApplicationModel> CreateApplication(CreateApplicationModel createDto);
        Task<bool> UpdateApplicationStatus(string id, string status, string userId);
    }
}
