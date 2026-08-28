using Npgsql;

namespace DBMigrationPostgres.Services.Interface;

public interface IPgMigration
{
    string MigrationId { get; }
    string Name { get; }
    string Description { get; }

    Task Up(NpgsqlConnection connection);
    Task Down(NpgsqlConnection connection); // Optional: for rollbacks
}
