using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using Eduflex.API.Models;

namespace Eduflex.API.Data
{
    public class MongoDbContext : DbContext
    {
        public MongoDbContext(DbContextOptions<MongoDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        //public DbSet<Course> Courses { get; set; }
        //public DbSet<Institution> Institutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToCollection("users");
            modelBuilder.Entity<Student>().ToCollection("students");
            //modelBuilder.Entity<Course>().ToCollection("courses");
            //modelBuilder.Entity<Institution>().ToCollection("institutions");
        }
    }
}