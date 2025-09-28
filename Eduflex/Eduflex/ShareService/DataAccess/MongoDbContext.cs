using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using ShareService.Models;

namespace ShareService.Data
{
    public class MongoDbContext : DbContext
    {
        public MongoDbContext(DbContextOptions<MongoDbContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<StudentModel> Students { get; set; }
        //public DbSet<Course> Courses { get; set; }
        //public DbSet<Institution> Institutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>().ToCollection("users");
            modelBuilder.Entity<StudentModel>().ToCollection("students");
            //modelBuilder.Entity<Course>().ToCollection("courses");
            //modelBuilder.Entity<Institution>().ToCollection("institutions");
        }
    }
}