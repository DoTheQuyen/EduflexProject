using DBMigration.Services.Interface;
using Spectre.Console;

namespace DBMigration;

public class ConsoleApp
{
    private readonly IDatabaseService _databaseService;
    private readonly IMigrationService _migrationService;
    private readonly string _environmentName;
    private readonly string _localSettingsPath;

    public ConsoleApp(IDatabaseService databaseService, IMigrationService migrationService, string environmentName, string localSettingsPath)
    {
        _databaseService = databaseService;
        _migrationService = migrationService;
        _environmentName = environmentName;
        _localSettingsPath = localSettingsPath;
    }

    private bool IsPro => _environmentName.Equals("Pro", StringComparison.OrdinalIgnoreCase);

    public async Task RunAsync()
    {
        if (!await _databaseService.TestConnectionAsync())
        {
            AnsiConsole.MarkupLine($"[red]Cannot connect to MongoDB with the current '{_environmentName}' connection settings.[/]");

            if (AnsiConsole.Confirm("Re-enter the connection string now?"))
            {
                var (connectionString, databaseName) = MongoConnectionStore.Prompt();
                MongoConnectionStore.Save(_localSettingsPath, _environmentName, connectionString, databaseName);
                AnsiConsole.MarkupLine("[green]✅ Saved.[/] [yellow]Restart the app (dotnet run) to connect with the new settings.[/]");
            }

            return;
        }

        while (true)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{(IsPro ? "red" : "grey")}]Environment: {_environmentName}[/]");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "0. Update Connection String for this environment",
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
                    case "0":
                        var (connectionString, databaseName) = MongoConnectionStore.Prompt();
                        MongoConnectionStore.Save(_localSettingsPath, _environmentName, connectionString, databaseName);
                        AnsiConsole.MarkupLine("[green]✅ Saved.[/] [yellow]Restart the app for the new connection to take effect.[/]");
                        break;

                    case "1":
                        await _databaseService.CreateCollectionsAsync();
                        await DisplayCurrentStateAsync();
                        break;

                    case "2":
                        if (ConfirmProAction("insert sample test data"))
                        {
                            await _databaseService.InsertTestDataAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "3":
                        await ModelGenerator.BuildDbModelsAsync(_databaseService);
                        break;

                    case "4":
                        await DisplayCurrentStateAsync();
                        break;

                    case "5":
                        if (ConfirmProAction("clear all test data"))
                        {
                            await _databaseService.ClearTestDataAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "6":
                        if (ConfirmProAction("drop all collections and delete all data"))
                        {
                            await _databaseService.DropCollectionsAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "7":
                        if (ConfirmProAction("run database migrations"))
                        {
                            if (await _migrationService.RunMigrationsAsync())
                            {
                                AnsiConsole.MarkupLine("[green]✅ Migrations completed successfully[/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[red]❌ Migrations failed[/]");
                            }
                        }
                        break;

                    case "8":
                        await DisplayMigrationHistoryAsync();
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

    private bool ConfirmProAction(string action)
    {
        if (IsPro)
        {
            AnsiConsole.MarkupLine($"[red]⚠ You are about to {action} on the PRO environment![/]");
            var typed = AnsiConsole.Ask<string>($"Type [bold]{_environmentName}[/] to confirm, or anything else to cancel:");
            return typed == _environmentName;
        }

        return AnsiConsole.Confirm($"Are you sure you want to {action} ({_environmentName})?");
    }

    private async Task DisplayMigrationHistoryAsync()
    {
        var history = await _migrationService.GetMigrationHistoryAsync();

        if (!history.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No migrations have been applied yet.[/]");
            return;
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
    }

    private async Task DisplayCurrentStateAsync()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]📊 Current Database State:[/]");

        var collections = await _databaseService.GetCollectionNamesAsync();

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
            var count = await _databaseService.GetCollectionCountAsync(collectionName);
            var status = count > 0 ? "✅ Has Data" : "⚠️ Empty";

            table.AddRow(
                collectionName,
                count.ToString("N0"),
                status
            );
        }

        AnsiConsole.Write(table);
    }
}
