using System.ComponentModel;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class HistorySettings : CommandSettings
{
    [CommandOption("--type")]
    [Description("Filtra por componente: cli, sqlserver, postgres")]
    public string? Type { get; set; }
}

public class HistoryCommand : AsyncCommand<HistorySettings>
{
    private static readonly IReadOnlyDictionary<string, string> ShortDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PackageIds.Cli]       = "CLI",
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
        string? component = null;

        if (!string.IsNullOrWhiteSpace(settings.Type) &&
            !PackageIds.TypeToId.TryGetValue(settings.Type, out component))
        {
            _console.MarkupLine(string.Format(SR.Current.InvalidTypeHistory, Markup.Escape(settings.Type)));
            return 1;
        }

        var entries = await _historyService.GetHistoryAsync(component, cancellationToken);

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
