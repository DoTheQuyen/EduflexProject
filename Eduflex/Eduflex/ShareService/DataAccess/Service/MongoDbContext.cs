using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using ShareService.DataAccess.Interface;
using ShareService.Models;

namespace ShareService.DataAccess.Service
{
    public class MongoDbContext : DbContext, IMongoDbContext
    {
        public MongoDbContext(DbContextOptions<MongoDbContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<StudentModel> Students { get; set; }
        public DbSet<ApplicationModel> Applications { get; set; }

        //public DbSet<CoursesModel> Courses { get; set; }
        //public DbSet<InstitutionsModel> Institutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>().ToCollection("Users");
            modelBuilder.Entity<StudentModel>().ToCollection("Students");
            modelBuilder.Entity<ApplicationModel>().ToCollection("Applications");
            //modelBuilder.Entity<Course>().ToCollection("courses");
            //modelBuilder.Entity<Institution>().ToCollection("institutions");
        }
    }
}