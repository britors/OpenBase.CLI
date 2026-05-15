using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands.Extension;

public class ExtensionListSettings : CommandSettings { }

public class ExtensionListCommand(
    IAnsiConsole console,
    ICsprojLocator csprojLocator,
    IProjectLocator projectLocator,
    IExtensionRegistry registry,
    IEnumerable<IExtensionHandler> handlers)
    : Command<ExtensionListSettings>
{
    protected override int Execute(CommandContext context, ExtensionListSettings settings, CancellationToken cancellationToken)
    {
        var workingDir = Directory.GetCurrentDirectory();

        var (solutionDir, _) = projectLocator.Detect(workingDir, null);

        string? projectDir = solutionDir;
        if (projectDir is null)
        {
            var csprojPath = csprojLocator.Find(workingDir);
            if (csprojPath is not null)
                projectDir = Path.GetDirectoryName(csprojPath);
        }

        var installed = projectDir is not null
            ? registry.GetAll(projectDir).Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn(SR.Current.ExtensionListColName);
        table.AddColumn(SR.Current.ExtensionListColCommand);
        table.AddColumn(SR.Current.ExtensionListColStatus);

        foreach (var name in handlers.Select(h => h.Name).OrderBy(n => n))
        {
            var status = installed.Contains(name)
                ? SR.Current.ExtensionListStatusInstalled
                : SR.Current.ExtensionListStatusAvailable;

            table.AddRow(
                $"[blue]{name}[/]",
                $"openbase extension add {name}",
                status);
        }

        console.Write(table);
        return 0;
    }
}
