using DBMigrationPostgres.Services.Services;

namespace DBMigrationPostgres.Migrations
{
    // Bootstrap migration — recreates the exact schema the old EF Core auto-migration
    // (InitialCreateDepartmentsRolesUsers) already applied to Local. Written idempotently
    // (IF NOT EXISTS everywhere) so re-running it against a database that already has these
    // tables is a safe no-op, and running it against a brand-new database builds them fresh.
    public class _001_InitialCreateDepartmentsRolesUsers_250826 : SafePgMigrationBase
    {
        public override string MigrationId => "001_InitialCreateDepartmentsRolesUsers_250826";
        public override string Name => "Initial Create Departments Roles Users";
        public override string Description => "Creates Departments, Roles, and Users tables, the Users->Roles foreign key (RESTRICT on delete), and the Email (unique) / RoleId indexes";

        protected override async Task UpCore()
        {
            await ExecuteSqlAsync("""
                CREATE TABLE IF NOT EXISTS "Departments" (
                    "Id" character varying(24) NOT NULL,
                    "Name" character varying(150) NOT NULL,
                    "Description" character varying(300),
                    "ParentDepartmentId" character varying(24),
                    "HeadUserId" character varying(24),
                    "MemberUserIds" text[] NOT NULL,
                    "CreatedBy" text,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedBy" text,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
                );
                """);

            await ExecuteSqlAsync("""
                CREATE TABLE IF NOT EXISTS "Roles" (
                    "Id" character varying(24) NOT NULL,
                    "Name" character varying(150) NOT NULL,
                    "Description" character varying(300) NOT NULL,
                    "RoleType" integer NOT NULL,
                    "PermissionIds" text[] NOT NULL,
                    "CreatedBy" text,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedBy" text,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
                );
                """);

            await ExecuteSqlAsync("""
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" character varying(24) NOT NULL,
                    "Email" character varying(256) NOT NULL,
                    "PasswordHash" text NOT NULL,
                    "FirstName" character varying(100) NOT NULL,
                    "MiddleName" character varying(100),
                    "LastName" character varying(100) NOT NULL,
                    "Mobile" character varying(30) NOT NULL,
                    "RoleId" character varying(24) NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "MustChangePassword" boolean NOT NULL,
                    "LastLogin" timestamp with time zone,
                    "ResetToken" text,
                    "ResetTokenExpiry" timestamp with time zone,
                    "CreatedBy" text,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedBy" text,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE RESTRICT
                );
                """);

            await CreateUniqueIndexAsync("IX_Users_Email", "Users", "Email");
            await CreateIndexAsync("IX_Users_RoleId", "Users", "RoleId");
        }

        protected override async Task DownCore()
        {
            await DropTableAsync("Users");
            await DropTableAsync("Roles");
            await DropTableAsync("Departments");
        }
    }
}
