using DBMigrationPostgres;
using DBMigrationPostgres.Services.Interface;
using DBMigrationPostgres.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareService.Common;
using ShareService.DataAccess.Postgres;
using Spectre.Console;

AnsiConsole.MarkupLine("[green]PostgreSQL Database Management Console. Developed by Quyen Do[/]");
AnsiConsole.WriteLine();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// appsettings.local.json lives next to appsettings.json in the project folder but is
// gitignored — each teammate keeps their own Test/Pro connection strings there instead
// of committing them. Mirrors DBMigration's MongoConnectionStore pattern exactly.
var localSettingsPath = PostgresConnectionStore.GetProjectLocalSettingsPath();
builder.Configuration.AddJsonFile(localSettingsPath, optional: true, reloadOnChange: true);

var environmentNames = builder.Configuration.GetSection("PostgresEnvironments")
    .GetChildren()
    .Select(section => section.Key)
    .ToList();

if (!environmentNames.Any())
{
    AnsiConsole.MarkupLine("[red]No environments found under 'PostgresEnvironments' in appsettings.json.[/]");
    return;
}

var selectedEnvironment = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Which Postgres server do you want to connect to?")
        .AddChoices(environmentNames));

var selectedSection = builder.Configuration.GetSection($"PostgresEnvironments:{selectedEnvironment}");
var connectionString = selectedSection["ConnectionString"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    AnsiConsole.MarkupLine($"[yellow]No connection configured yet for '{selectedEnvironment}'.[/]");
    connectionString = PostgresConnectionStore.Prompt();
    PostgresConnectionStore.Save(localSettingsPath, selectedEnvironment, connectionString);
    AnsiConsole.MarkupLine($"[green]✅ Saved — this will be reused automatically next time you pick '{selectedEnvironment}'.[/]");
}

var environmentColor = selectedEnvironment.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? "red" : "yellow";
AnsiConsole.MarkupLine($"[{environmentColor}]➡ Connected environment: {selectedEnvironment}[/]");
AnsiConsole.WriteLine();

builder.Services.AddDbContext<EduflexPostgresContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("DBMigrationPostgres")));

builder.Services.AddSingleton<ICurrentUserService, ConsoleCurrentUserService>();
builder.Services.AddScoped<IPostgresDatabaseService, PostgresDatabaseService>();
builder.Services.AddScoped<IPostgresMigrationService, PostgresMigrationService>();

builder.Services.AddLogging();

using var host = builder.Build();

var databaseService = host.Services.GetRequiredService<IPostgresDatabaseService>();
var migrationService = host.Services.GetRequiredService<IPostgresMigrationService>();

var consoleApp = new PostgresConsoleApp(databaseService, migrationService, selectedEnvironment, localSettingsPath);
await consoleApp.RunAsync();
