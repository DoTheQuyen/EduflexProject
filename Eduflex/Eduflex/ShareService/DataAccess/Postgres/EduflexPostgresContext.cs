using Microsoft.EntityFrameworkCore;
using ShareService.Models.Auth;
using ShareService.Models.Department;
using ShareService.Models.Role;

namespace ShareService.DataAccess.Postgres
{
    public class EduflexPostgresContext : DbContext
    {
        public EduflexPostgresContext(DbContextOptions<EduflexPostgresContext> options)
            : base(options)
        {
        }

        public DbSet<DepartmentModel> Departments => Set<DepartmentModel>();
        public DbSet<RoleModel> Roles => Set<RoleModel>();
        public DbSet<UserModel> Users => Set<UserModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DepartmentModel>(entity =>
            {
                entity.ToTable("Departments");

                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id).HasMaxLength(24).ValueGeneratedNever();

                entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
                entity.Property(d => d.Description).HasMaxLength(300);
                entity.Property(d => d.ParentDepartmentId).HasMaxLength(24);
                entity.Property(d => d.HeadUserId).HasMaxLength(24);
                entity.Property(d => d.MemberUserIds).HasColumnType("text[]");
            });

            modelBuilder.Entity<RoleModel>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasMaxLength(24).ValueGeneratedNever();

                entity.Property(r => r.Name).IsRequired().HasMaxLength(150);
                entity.Property(r => r.Description).HasMaxLength(300);
                entity.Property(r => r.PermissionIds).HasColumnType("text[]");

                entity.Ignore(r => r.UserCount);
            });

            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasMaxLength(24).ValueGeneratedNever();

                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.MiddleName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.Mobile).IsRequired().HasMaxLength(30);
                entity.Property(u => u.RoleId).HasMaxLength(24);

                entity.HasOne<RoleModel>()
                    .WithMany()
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Mobile);
                entity.HasIndex(u => new { u.CreatedAt, u.Id });
                entity.Ignore(u => u.Password);
            });
        }
    }
}