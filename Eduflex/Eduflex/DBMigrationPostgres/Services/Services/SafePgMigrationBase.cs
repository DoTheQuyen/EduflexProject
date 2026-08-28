using DBMigrationPostgres.Services.Interface;
using Npgsql;

namespace DBMigrationPostgres.Services.Services;

public abstract class SafePgMigrationBase : IPgMigration
{
    protected NpgsqlConnection Connection { get; private set; } = null!;

    public abstract string MigrationId { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }

    public async Task Up(NpgsqlConnection connection)
    {
        Connection = connection;
        await UpCore();
    }

    public async Task Down(NpgsqlConnection connection)
    {
        Connection = connection;
        await DownCore();
    }

    protected abstract Task UpCore();
    protected abstract Task DownCore();

    protected async Task<bool> TableExistsAsync(string tableName)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName)",
            Connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    protected async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @tableName AND column_name = @columnName)",
            Connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("columnName", columnName);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    protected async Task<bool> IndexExistsAsync(string indexName)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = @indexName)",
            Connection);
        cmd.Parameters.AddWithValue("indexName", indexName);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    protected async Task<bool> ConstraintExistsAsync(string constraintName)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_schema = 'public' AND constraint_name = @constraintName)",
            Connection);
        cmd.Parameters.AddWithValue("constraintName", constraintName);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    protected async Task ExecuteSqlAsync(string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, Connection);
        await cmd.ExecuteNonQueryAsync();
    }

    protected async Task AddColumnAsync(string tableName, string columnName, string sqlType, bool nullable = true)
    {
        if (await ColumnExistsAsync(tableName, columnName))
        {
            return;
        }

        var nullability = nullable ? "" : " NOT NULL";
        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {sqlType}{nullability};");
    }

    protected async Task DropColumnAsync(string tableName, string columnName)
    {
        if (!await ColumnExistsAsync(tableName, columnName))
        {
            return;
        }

        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" DROP COLUMN \"{columnName}\";");
    }

    protected async Task AlterColumnTypeAsync(string tableName, string columnName, string newSqlType)
    {
        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" TYPE {newSqlType};");
    }

    protected async Task SetColumnNullableAsync(string tableName, string columnName, bool nullable)
    {
        var action = nullable ? "DROP NOT NULL" : "SET NOT NULL";
        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" {action};");
    }

    protected async Task RenameColumnAsync(string tableName, string columnName, string newColumnName)
    {
        if (!await ColumnExistsAsync(tableName, columnName))
        {
            return;
        }

        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{columnName}\" TO \"{newColumnName}\";");
    }

    // onDelete/onUpdate take raw SQL referential actions: RESTRICT, CASCADE, SET NULL,
    // SET DEFAULT, or NO ACTION (Postgres's default when omitted).
    protected async Task AddForeignKeyAsync(
        string constraintName,
        string tableName,
        string columnName,
        string principalTableName,
        string principalColumnName,
        string onDelete = "NO ACTION",
        string onUpdate = "NO ACTION")
    {
        if (await ConstraintExistsAsync(constraintName))
        {
            return;
        }

        await ExecuteSqlAsync($"""
            ALTER TABLE "{tableName}"
            ADD CONSTRAINT "{constraintName}"
            FOREIGN KEY ("{columnName}") REFERENCES "{principalTableName}" ("{principalColumnName}")
            ON DELETE {onDelete} ON UPDATE {onUpdate};
            """);
    }

    protected async Task DropForeignKeyAsync(string tableName, string constraintName)
    {
        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" DROP CONSTRAINT IF EXISTS \"{constraintName}\";");
    }

    protected async Task CreateIndexAsync(string indexName, string tableName, params string[] columnNames)
    {
        var columns = string.Join(", ", columnNames.Select(name => $"\"{name}\""));
        await ExecuteSqlAsync($"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" ({columns});");
    }

    protected async Task CreateUniqueIndexAsync(string indexName, string tableName, params string[] columnNames)
    {
        var columns = string.Join(", ", columnNames.Select(name => $"\"{name}\""));
        await ExecuteSqlAsync($"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" ({columns});");
    }

    protected async Task DropIndexAsync(string indexName)
    {
        await ExecuteSqlAsync($"DROP INDEX IF EXISTS \"{indexName}\";");
    }

    protected async Task RenameTableAsync(string tableName, string newTableName)
    {
        if (!await TableExistsAsync(tableName))
        {
            return;
        }

        await ExecuteSqlAsync($"ALTER TABLE \"{tableName}\" RENAME TO \"{newTableName}\";");
    }

    // CASCADE also drops anything depending on this table (foreign keys pointing at it,
    // views built on it) — matching how the EF-generated DropTable behaved.
    protected async Task DropTableAsync(string tableName)
    {
        await ExecuteSqlAsync($"DROP TABLE IF EXISTS \"{tableName}\" CASCADE;");
    }
}
