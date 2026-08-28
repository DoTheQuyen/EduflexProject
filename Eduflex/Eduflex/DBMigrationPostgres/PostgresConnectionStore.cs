using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DBMigrationPostgres;

public static class PostgresConnectionStore
{
    public const string LocalSettingsFileName = "appsettings.local.json";

    // Project-root-relative, not bin-output-relative, so it survives `dotnet clean` —
    // mirrors DBMigration's MongoConnectionStore.GetProjectLocalSettingsPath convention.
    public static string GetProjectLocalSettingsPath()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
        return Path.Combine(projectRoot, LocalSettingsFileName);
    }

    public static string Prompt()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Enter Postgres connection string:")
                .Secret()
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("[red]Connection string cannot be empty[/]")
                    : ValidationResult.Success()));
    }

    // Merges into appsettings.local.json (gitignored) instead of overwriting it, so
    // one teammate saving "Test" doesn't wipe out another teammate's saved "Pro" entry.
    public static void Save(string localSettingsPath, string environmentName, string connectionString)
    {
        var root = File.Exists(localSettingsPath)
            ? JsonNode.Parse(File.ReadAllText(localSettingsPath)) as JsonObject ?? new JsonObject()
            : new JsonObject();

        if (root["PostgresEnvironments"] is not JsonObject environments)
        {
            environments = new JsonObject();
            root["PostgresEnvironments"] = environments;
        }

        environments[environmentName] = new JsonObject
        {
            ["ConnectionString"] = connectionString
        };

        File.WriteAllText(localSettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
