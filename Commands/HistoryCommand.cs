using System.ComponentModel;
using OpenBase.CLI.Helpers.Creation;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class HistorySettings : CommandSettings
{
    [CommandOption("--json")]
    public bool Json { get; set; }
    [CommandOption("--type")]
    [Description("Filtra por componente: template, cli, sqlserver, postgres, oracle")]
    public string? Type { get; set; }

    [CommandOption("--clear")]
    [Description("Limpa todo o histórico de atualizações")]
    public bool Clear { get; set; }
}

public class HistoryCommand : AsyncCommand<HistorySettings>
{
    private static readonly IReadOnlyDictionary<string, string> ShortDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PackageIds.Cli]       = "CLI",
            [PackageIds.Unified] = "OpenBaseNET",
            [PackageIds.Oracle] = "Oracle",
            [PackageIds.SqlServer] = "SQLServer",
            [PackageIds.Postgres]  = "Postgres",
        };

    private readonly IUpdateHistoryService _historyService;
    private readonly IAnsiConsole _console;

    public HistoryCommand(IUpdateHistoryService historyService, IAnsiConsole console)
    {
        _historyService = historyService;
        _console = console;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        HistorySettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.Clear)
        {
            await _historyService.ClearHistoryAsync(cancellationToken);
            if (settings.Json) return CommandResult.Write(_console, true, "history", new { cleared = true });
            _console.MarkupLine(SR.Current.HistoryCleared);
            return 0;
        }

        string? component = null;

        if (!string.IsNullOrWhiteSpace(settings.Type) &&
            !PackageIds.TypeToId.TryGetValue(settings.Type, out component))
        {
            return CommandResult.Write(_console, settings.Json, "history", error: new CliException("COMPONENT_UNSUPPORTED", "Use template, cli, postgres, sqlserver ou oracle."));
        }

        var entries = await _historyService.GetHistoryAsync(component, cancellationToken);

        if (settings.Json) return CommandResult.Write(_console, true, "history", new { entries });

        if (entries.Count == 0)
        {
            _console.MarkupLine(SR.Current.NoHistoryFound);
            return 0;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(SR.Current.ColDate);
        table.AddColumn(SR.Current.ColComponent);
        table.AddColumn(SR.Current.ColPreviousVersion);
        table.AddColumn(SR.Current.ColNewVersion);
        table.AddColumn(SR.Current.ColStatus);

        foreach (var entry in entries)
        {
            var displayName = entry.Component is not null && ShortDisplayNames.TryGetValue(entry.Component, out var name)
                ? name
                : entry.Component ?? "-";

            var date   = entry.Date.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var prev   = entry.PreviousVersion ?? "[grey]-[/]";
            var next   = entry.NewVersion ?? "[grey]-[/]";
            var status = entry.Success ? "[green]✓[/]" : "[red]✗[/]";

            table.AddRow(date, displayName, prev, next, status);
        }

        _console.Write(table);
        return 0;
    }
}
