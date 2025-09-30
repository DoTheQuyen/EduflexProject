using Microsoft.EntityFrameworkCore;
using ShareService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareService.DataAccess.Interface
{
    public interface IMongoDbContext
    {
        DbSet<UserModel> Users { get; set; }
        DbSet<StudentModel> Students { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
