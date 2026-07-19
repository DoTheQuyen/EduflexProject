using DBMigration.Migrations;
using DBMigration.Services.Interface;
using DBMigration.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Spectre.Console;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ShareService.Models.Setting;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configure MongoDBSettings properly
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register MongoDBSettings as a singleton for direct injection
builder.Services.AddSingleton<MongoDBSettings>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var settings = new MongoDBSettings();
    configuration.GetSection("MongoDB").Bind(settings);
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

// Register migration services
builder.Services.AddScoped<IMigrationService, MigrationService>();

// Register all migrations
builder.Services.AddTransient<_001_AddApplications_290925>();
builder.Services.AddTransient<_002_AddUserLastLoginField_290925>();
builder.Services.AddTransient<_004_AddUserForeignKeyToStudents_290925>();
builder.Services.AddTransient<_005_AddStudentForeignKeyToApplications_290925>();
builder.Services.AddTransient<_006_ConvertStudentIdToString_290925>();
builder.Services.AddTransient<_007_AddEnquiriesCollection_170726>();
builder.Services.AddTransient<_008_AddFeedbacksCollection_170726>();
builder.Services.AddTransient<_009_AddCoursePromotionsCollection_180726>();

// Register your services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// Add logging
builder.Services.AddLogging();

using var host = builder.Build();

await RunConsoleApp(host.Services);

async Task RunConsoleApp(IServiceProvider services)
{
    var databaseService = services.GetRequiredService<IDatabaseService>();

    AnsiConsole.Write(new FigletText("EduFlex DB Manager").Color(Color.Blue));
    AnsiConsole.MarkupLine("[green]MongoDB Database Management Console. Developed by Quyen Do[/]");
    AnsiConsole.WriteLine();

    // Test connection first
    if (!await databaseService.TestConnectionAsync())
    {
        AnsiConsole.MarkupLine("[red]Cannot connect to MongoDB. Please check your connection settings.[/]");
        return;
    }

    while (true)
    {
        AnsiConsole.WriteLine();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(new[] {
                    "1. Create Database Tables (Collections)",
                    "2. Insert Test Data",
                    "3. Generate C# Models",
                    "4. View Current Collections",
                    "5. Clear Test Data",
                    "6. Drop All Collections",
                    "7. Run Database Migrations", 
                    "8. View Migration History", 
                    "9. Exit"
                }));

        try
        {
            switch (choice.Substring(0, 1))
            {
                case "1":
                    await databaseService.CreateCollectionsAsync();
                    await DisplayCurrentState(databaseService);
                    break;

                case "2":
                    await databaseService.InsertTestDataAsync();
                    await DisplayCurrentState(databaseService);
                    break;

                case "3":
                    await BuildDBModels(databaseService);
                    break;

                case "4":
                    await DisplayCurrentState(databaseService);
                    break;

                case "5":
                    if (AnsiConsole.Confirm("Are you sure you want to clear all test data?"))
                    {
                        await databaseService.ClearTestDataAsync();
                        await DisplayCurrentState(databaseService);
                    }
                    break;

                case "6":
                    if (AnsiConsole.Confirm("[red]WARNING: This will drop all collections and delete all data![/]"))
                    {
                        await databaseService.DropCollectionsAsync();
                        await DisplayCurrentState(databaseService);
                    }
                    break;

                case "7":
                    var migrationService = services.GetRequiredService<IMigrationService>();
                    if (await migrationService.RunMigrationsAsync())
                    {
                        AnsiConsole.MarkupLine("[green]✅ Migrations completed successfully[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Migrations failed[/]");
                    }
                    break;

                case "8":
                    var migrationHistoryService = services.GetRequiredService<IMigrationService>();
                    var history = await migrationHistoryService.GetMigrationHistoryAsync();

                    if (!history.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No migrations have been applied yet.[/]");
                        break;
                    }

                    var historyTable = new Table();
                    historyTable.AddColumn("Migration ID");
                    historyTable.AddColumn("Name");
                    historyTable.AddColumn("Applied At");
                    historyTable.AddColumn("Status");
                    historyTable.AddColumn("Execution Time");

                    foreach (var record in history)
                    {
                        historyTable.AddRow(
                            record.MigrationId,
                            record.Name,
                            record.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                            record.Success ? "✅ Success" : "❌ Failed",
                            $"{record.ExecutionTimeMs}ms"
                        );
                    }

                    AnsiConsole.Write(historyTable);
                    break;

                case "9":
                    AnsiConsole.MarkupLine("[green]Goodbye! 👋[/]");
                    return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }
}

async Task BuildDBModels(IDatabaseService databaseService)
{
    string baseDir = AppContext.BaseDirectory;
    string rootDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
    string outputDir = Path.Combine(rootDir, "ShareService", "Models");
    Directory.CreateDirectory(outputDir);

    var collections = await databaseService.GetCollectionNamesAsync();
    if (collections == null || !collections.Any())
    {
        Console.WriteLine("⚠️ No collections found in the database.");
        return;
    }
    Console.WriteLine($"📊 Found {collections.Count} collections: {string.Join(", ", collections)}");

    foreach (var collectionName in collections)
    {
        try
        {
            var collection = databaseService.GetCollection(collectionName);
            if (collection == null)
            {
                Console.WriteLine($"⚠️ Collection '{collectionName}' not found or inaccessible.");
                continue;
            }
            else
            {
                string path = Path.Combine(outputDir, $"{collectionName}Model.cs");

                string classContent = GenerateCSharpClass(collectionName, collection.ToBsonDocument());

                bool shouldWrite = true;
                if (File.Exists(path))
                {
                    string existingContent = await File.ReadAllTextAsync(path);
                    if (existingContent == classContent)
                    {
                        Console.WriteLine($"✅ Model unchanged: {Path.GetFileName(path)}");
                        shouldWrite = false;
                    }
                    else
                    {
                        Console.WriteLine($"⚡ Updating model: {Path.GetFileName(path)}");
                    }
                }
                else
                {
                    Console.WriteLine($"✅ Creating new model: {Path.GetFileName(path)}");
                }

                if (shouldWrite)
                {
                    await File.WriteAllTextAsync(path, classContent);
                    Console.WriteLine($"✅ Generated: {Path.GetFileName(path)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing collection {collectionName}: {ex.Message}");
            continue;
        }
       
    }
}

static string GenerateCSharpClass(string name, BsonDocument doc)
{
    var props = new List<string>();
    if (doc?.Elements == null)
    {
        return string.Empty;
    }

    foreach (var element in doc.Elements)
    {
        
        if (element.Name == "_id")
        {
            props.Add($@"    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {{ get; set; }}");
        }
        else
        {
            string clrType = MapBsonTypeToCSharp(element.Value.BsonType);
            props.Add($@"    [BsonElement(""{element.Name}"")]
    public {clrType} {ToPascalCase(element.Name)} {{ get; set; }}");
        }
    }

    return
$@"using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Eduflex.Shared.Models;

public class {ToPascalCase(name)}
{{
{string.Join(Environment.NewLine + Environment.NewLine, props)}
}}";
}

static string MapBsonTypeToCSharp(BsonValue value)
{
    // Prefer value-based inference (arrays, etc.)
    switch (value.BsonType)
    {
        case BsonType.ObjectId: return "string";          // store as string with [BsonRepresentation]
        case BsonType.String: return "string";
        case BsonType.Int32: return "int";
        case BsonType.Int64: return "long";
        case BsonType.Double: return "double";
        case BsonType.Boolean: return "bool";
        case BsonType.DateTime: return "DateTime";
        case BsonType.Decimal128: return "decimal";
        case BsonType.Document: return "BsonDocument";    // or a nested POCO if you generate one
        case BsonType.Array:
            var arr = value.AsBsonArray;
            var elemType = arr.Count > 0 ? MapBsonTypeToCSharp(arr[0]) : "object";
            return elemType.EndsWith("[]", StringComparison.Ordinal) ? elemType : $"{elemType}[]";
        case BsonType.Binary: return "byte[]";
        case BsonType.Null: return "string";          // fallback
        default: return "string";          // safe default
    }
}


static string ToPascalCase(string fieldName)
{
    // split on non-alphanumerics and join in PascalCase
    var parts = Regex.Split(fieldName ?? string.Empty, @"[^A-Za-z0-9]+")
                     .Where(p => p.Length > 0)
                     .Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1));
    var name = string.Concat(parts);
    if (string.IsNullOrEmpty(name)) name = "Field";
    if (char.IsDigit(name[0])) name = "_" + name;
    // escape C# keywords
    if (new[] { "Class", "Namespace", "Event", "String", "Int", "Long", "Decimal", "Object", "Params", "Operator", "Base", "This" }
        .Contains(name, StringComparer.Ordinal)) name = "@" + name;
    return name;
}

async Task DisplayCurrentState(IDatabaseService databaseService)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[yellow]📊 Current Database State:[/]");

    var collections = await databaseService.GetCollectionNamesAsync();

    if (!collections.Any())
    {
        AnsiConsole.MarkupLine("[red]No collections found in the database.[/]");
        return;
    }

    var table = new Table();
    table.AddColumn("Collection Name");
    table.AddColumn("Document Count");
    table.AddColumn("Status");

    foreach (var collectionName in collections.OrderBy(name => name))
    {
        var count = await databaseService.GetCollectionCountAsync(collectionName);
        var status = count > 0 ? "✅ Has Data" : "⚠️ Empty";

        table.AddRow(
            collectionName,
            count.ToString("N0"),
            status
        );
    }

    AnsiConsole.Write(table);
}