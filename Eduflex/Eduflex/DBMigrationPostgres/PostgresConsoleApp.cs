using DBMigrationPostgres.Services.Interface;
using Spectre.Console;

namespace DBMigrationPostgres;

public class PostgresConsoleApp
{
    private readonly IPostgresDatabaseService _databaseService;
    private readonly IPostgresMigrationService _migrationService;
    private readonly string _environmentName;
    private readonly string _localSettingsPath;

    public PostgresConsoleApp(
        IPostgresDatabaseService databaseService,
        IPostgresMigrationService migrationService,
        string environmentName,
        string localSettingsPath)
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
            AnsiConsole.MarkupLine($"[red]Cannot connect to Postgres with the current '{_environmentName}' connection settings.[/]");

            if (AnsiConsole.Confirm("Re-enter the connection string now?"))
            {
                var connectionString = PostgresConnectionStore.Prompt();
                PostgresConnectionStore.Save(_localSettingsPath, _environmentName, connectionString);
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
                    .AddChoices(new[]
                    {
                        "0. Update Connection String for this environment",
                        "1. Run Database Migrations",
                        "2. View Migration Status",
                        "3. Insert Test Data (department users)",
                        "4. Clear Test Data",
                        "5. View Current Tables",
                        "6. Drop All Tables",
                        "7. Exit"
                    }));

            try
            {
                switch (choice.Substring(0, choice.IndexOf('.')))
                {
                    case "0":
                        var connectionString = PostgresConnectionStore.Prompt();
                        PostgresConnectionStore.Save(_localSettingsPath, _environmentName, connectionString);
                        AnsiConsole.MarkupLine("[green]✅ Saved.[/] [yellow]Restart the app for the new connection to take effect.[/]");
                        break;

                    case "1":
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

                    case "2":
                        await DisplayMigrationStatusAsync();
                        break;

                    case "3":
                        if (ConfirmProAction("insert department test data"))
                        {
                            await _databaseService.InsertTestDataAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "4":
                        if (ConfirmProAction("clear the seeded department test data"))
                        {
                            await _databaseService.ClearTestDataAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "5":
                        await DisplayCurrentStateAsync();
                        break;

                    case "6":
                        if (ConfirmProAction("drop all tables and delete all data"))
                        {
                            await _databaseService.DropAllTablesAsync();
                            await DisplayCurrentStateAsync();
                        }
                        break;

                    case "7":
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

    private async Task DisplayMigrationStatusAsync()
    {
        var applied = await _migrationService.GetAppliedMigrationsAsync();
        var pending = await _migrationService.GetPendingMigrationsAsync();

        if (applied.Count == 0 && pending.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No migrations found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Migration");
        table.AddColumn("Status");

        foreach (var id in applied)
        {
            table.AddRow(id, "✅ Applied");
        }

        foreach (var id in pending)
        {
            table.AddRow(id, "⏳ Pending");
        }

        AnsiConsole.Write(table);
    }

    private async Task DisplayCurrentStateAsync()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]📊 Current Database State:[/]");

        var tableNames = await _databaseService.GetTableNamesAsync();

        if (tableNames.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No tables found in the database.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Table Name");
        table.AddColumn("Row Count");
        table.AddColumn("Status");

        foreach (var tableName in tableNames.OrderBy(name => name))
        {
            var count = await _databaseService.GetTableRowCountAsync(tableName);
            var status = count > 0 ? "✅ Has Data" : "⚠️ Empty";

            table.AddRow(tableName, count.ToString("N0"), status);
        }

        AnsiConsole.Write(table);
    }
}
