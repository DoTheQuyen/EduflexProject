namespace DBMigrationPostgres.Models;

public class ImportComparisonRow
{
    public string EntityName { get; set; } = string.Empty;
    public long MongoCount { get; set; }
    public long PostgresCount { get; set; }
    public int MissingInPostgres { get; set; }
    public int FieldMismatches { get; set; }

    public bool IsMatched => MissingInPostgres == 0 && FieldMismatches == 0 && MongoCount == PostgresCount;
}
