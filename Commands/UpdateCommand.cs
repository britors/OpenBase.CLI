using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class UpdateSettings : CommandSettings
{
}

public class UpdateCommand : AsyncCommand<UpdateSettings>
{
    private readonly ITemplatePackageRunner _packageRunner;
    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;
    private readonly IUpdateHistoryService _historyService;

    public UpdateCommand(
        ITemplatePackageRunner packageRunner,
        IDotNetRunner dotNetRunner,
        IAnsiConsole console,
        IUpdateHistoryService historyService)
    {
        _packageRunner = packageRunner;
        _dotNetRunner = dotNetRunner;
        _console = console;
        _historyService = historyService;
    }

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] UpdateSettings settings,
        CancellationToken cancellationToken)
    {
        var previousCliVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

        var previousTemplateVersions = new Dictionary<string, string?>();
        foreach (var pkg in PackageIds.Templates)
            previousTemplateVersions[pkg] = await _dotNetRunner.GetInstalledTemplateVersionAsync(pkg, cancellationToken);

        _console.MarkupLine(SR.Current.SyncingTemplates);
        var packageResults = await _packageRunner.RunPackagesAsync(
            SR.Current.PackageStatusVerb, SR.Current.PackageSuccessLabel, SR.Current.PackageErrorLabel, cancellationToken);
        _console.WriteLine();

        var newTemplateVersions = new Dictionary<string, string?>();
        foreach (var (pkgId, _) in packageResults.Where(r => r.Success))
            newTemplateVersions[pkgId] = await _dotNetRunner.GetInstalledTemplateVersionAsync(pkgId, cancellationToken);

        string? newCliVersion = null;
        var cliFailed = false;

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(SR.Current.UpdatingCli, async _ =>
            {
                var (success, error) = await _dotNetRunner.RunAsync($"tool update -g {PackageIds.Cli}", cancellationToken);
                if (!success)
                {
                    cliFailed = true;
                    _console.MarkupLine(SR.Current.UpdateCliFailed);
                    if (!string.IsNullOrWhiteSpace(error))
                        _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    _console.MarkupLine(SR.Current.SomeComponentsUpdateFailed);
                }
                else
                {
                    newCliVersion = await _dotNetRunner.GetInstalledToolVersionAsync(PackageIds.Cli, cancellationToken);
                    _console.MarkupLine(SR.Current.CliUpdated);
                }
            });

        var now = DateTime.UtcNow;

        foreach (var (pkgId, success) in packageResults)
        {
            await _historyService.AddEntryAsync(new UpdateHistoryEntry
            {
                Date            = now,
                Component       = pkgId,
                PreviousVersion = previousTemplateVersions.GetValueOrDefault(pkgId),
                NewVersion      = success ? newTemplateVersions.GetValueOrDefault(pkgId) : null,
                Success         = success
            }, cancellationToken);
        }

        await _historyService.AddEntryAsync(new UpdateHistoryEntry
        {
            Date            = now,
            Component       = PackageIds.Cli,
            PreviousVersion = previousCliVersion,
            NewVersion      = newCliVersion,
            Success         = !cliFailed
        }, cancellationToken);

        var failed = packageResults.Any(r => !r.Success) || cliFailed;
        return failed ? 1 : 0;
    }
}
