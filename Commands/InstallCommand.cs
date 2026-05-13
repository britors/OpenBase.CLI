using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class InstallSettings : CommandSettings
{
}

public class InstallCommand : AsyncCommand<InstallSettings>
{
    private readonly ITemplatePackageRunner _runner;

    public InstallCommand(ITemplatePackageRunner runner)
    {
        _runner = runner;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        InstallSettings settings,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine(SR.Current.InstallStarting);

        var results = await _runner.RunPackagesAsync(
            SR.Current.InstallStatusVerb, SR.Current.InstallSuccessLabel, SR.Current.InstallErrorLabel, cancellationToken);

        return results.Any(r => !r.Success) ? 1 : 0;
    }
}
