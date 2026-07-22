using DBMigration;
using DBMigration.Services.Interface;
using DBMigration.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using ShareService.Models.Setting;
using Spectre.Console;

AnsiConsole.Write(new FigletText("EduFlex DB Manager").Color(Color.Blue));
AnsiConsole.MarkupLine("[green]MongoDB Database Management Console. Developed by Quyen Do[/]");
AnsiConsole.WriteLine();

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// appsettings.local.json lives next to appsettings.json in the project folder but is
// gitignored — each teammate keeps their own Test/Pro connection strings there instead
// of committing them or relying on a per-Windows-user secrets store.
var localSettingsPath = MongoConnectionStore.GetProjectLocalSettingsPath();
builder.Configuration.AddJsonFile(localSettingsPath, optional: true, reloadOnChange: true);

// Ask which MongoDB server to target before wiring up any DB services
var environmentNames = builder.Configuration.GetSection("MongoDBEnvironments")
    .GetChildren()
    .Select(section => section.Key)
    .ToList();

if (!environmentNames.Any())
{
    AnsiConsole.MarkupLine("[red]No environments found under 'MongoDBEnvironments' in appsettings.json.[/]");
    return;
}

var selectedEnvironment = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Which MongoDB server do you want to connect to?")
        .AddChoices(environmentNames));

var selectedSection = builder.Configuration.GetSection($"MongoDBEnvironments:{selectedEnvironment}");
if (string.IsNullOrWhiteSpace(selectedSection["ConnectionString"]))
{
    AnsiConsole.MarkupLine($"[yellow]No connection configured yet for '{selectedEnvironment}'.[/]");
    var (connectionString, databaseName) = MongoConnectionStore.Prompt();
    MongoConnectionStore.Save(localSettingsPath, selectedEnvironment, connectionString, databaseName);

    // Apply immediately so this run doesn't need a restart to pick up what was just entered.
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        [$"MongoDBEnvironments:{selectedEnvironment}:ConnectionString"] = connectionString,
        [$"MongoDBEnvironments:{selectedEnvironment}:DatabaseName"] = databaseName
    });
    selectedSection = builder.Configuration.GetSection($"MongoDBEnvironments:{selectedEnvironment}");

    AnsiConsole.MarkupLine($"[green]✅ Saved — this will be reused automatically next time you pick '{selectedEnvironment}'.[/]");
}

var environmentColor = selectedEnvironment.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? "red" : "yellow";
AnsiConsole.MarkupLine($"[{environmentColor}]➡ Connected environment: {selectedEnvironment}[/]");
AnsiConsole.WriteLine();

// Configure MongoDBSettings from the chosen environment's section
builder.Services.Configure<MongoDBSettings>(selectedSection);

// Register MongoDBSettings as a singleton for direct injection
builder.Services.AddSingleton<MongoDBSettings>(serviceProvider =>
{
    var settings = new MongoDBSettings();
    selectedSection.Bind(settings);
    return settings;
});

// Register MongoDB services
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<MongoDBSettings>();
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = serviceProvider.GetRequiredService<MongoDBSettings>();
    return client.GetDatabase(settings.DatabaseName);
});

// Migrations are discovered by reflection (any DBMigration.Migrations class starting with "_")
// and registered here — see MigrationRegistrationService. No manual list to maintain.
var migrationRegistrationService = new MigrationRegistrationService();
migrationRegistrationService.RegisterMigrations(builder.Services);
builder.Services.AddSingleton<IMigrationRegistrationService>(migrationRegistrationService);

builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// Add logging
builder.Services.AddLogging();

using var host = builder.Build();

var databaseService = host.Services.GetRequiredService<IDatabaseService>();
var migrationService = host.Services.GetRequiredService<IMigrationService>();

var consoleApp = new ConsoleApp(databaseService, migrationService, selectedEnvironment, localSettingsPath);
await consoleApp.RunAsync();
