using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DBMigration;

public static class MongoConnectionStore
{
    public const string LocalSettingsFileName = "appsettings.local.json";

    // Project-root-relative, not bin-output-relative, so it survives `dotnet clean`
    // and is easy for anyone on the team to find/edit — matches ModelGenerator's
    // "..\..\..\.." climb-out-of-bin\Debug\net8.0 convention used elsewhere in this project.
    public static string GetProjectLocalSettingsPath()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
        return Path.Combine(projectRoot, LocalSettingsFileName);
    }

    public static (string ConnectionString, string DatabaseName) Prompt()
    {
        var connectionString = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter MongoDB connection string:")
                .Secret()
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("[red]Connection string cannot be empty[/]")
                    : ValidationResult.Success()));

        var databaseName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter database name:")
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("[red]Database name cannot be empty[/]")
                    : ValidationResult.Success()));

        return (connectionString, databaseName);
    }

    // Merges into appsettings.local.json (gitignored) instead of overwriting it, so
    // one teammate saving "Test" doesn't wipe out another teammate's saved "Pro" entry.
    public static void Save(string localSettingsPath, string environmentName, string connectionString, string databaseName)
    {
        var root = File.Exists(localSettingsPath)
            ? JsonNode.Parse(File.ReadAllText(localSettingsPath)) as JsonObject ?? new JsonObject()
            : new JsonObject();

        if (root["MongoDBEnvironments"] is not JsonObject environments)
        {
            environments = new JsonObject();
            root["MongoDBEnvironments"] = environments;
        }

        environments[environmentName] = new JsonObject
        {
            ["ConnectionString"] = connectionString,
            ["DatabaseName"] = databaseName
        };

        File.WriteAllText(localSettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
