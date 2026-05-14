using System.ComponentModel;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands.Extension;

public class ExtensionAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("Name of the extension to add (e.g.: jwt, cache, blob)")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-p|--provider <provider>")]
    [Description("Provider variant of the extension (e.g.: redis, azure)")]
    public string? Provider { get; set; }
}

public class ExtensionAddCommand(
    IAnsiConsole console,
    ICsprojLocator csprojLocator,
    ICsprojPackageReader packageReader,
    IProjectLocator projectLocator,
    IExtensionRegistry registry,
    IEnumerable<IExtensionHandler> handlers)
    : Command<ExtensionAddSettings>
{
    protected override int Execute(CommandContext context, ExtensionAddSettings settings, CancellationToken cancellationToken)
    {
        var workingDir = Directory.GetCurrentDirectory();

        // Prefer OpenBase solution structure (sln + *.Domain)
        var (solutionDir, rootNamespace) = projectLocator.Detect(workingDir, null);

        // Fall back to nearest .csproj for non-OpenBase projects
        string? csprojPath = null;
        if (solutionDir is null)
        {
            csprojPath = csprojLocator.Find(workingDir);
            if (csprojPath is null)
            {
                console.MarkupLine(SR.Current.ExtensionNoCsprojFound);
                return 1;
            }
        }

        var projectDir = solutionDir ?? Path.GetDirectoryName(csprojPath)!;

        if (registry.IsInstalled(projectDir, settings.Name, settings.Provider))
        {
            console.MarkupLine(string.Format(SR.Current.ExtensionAlreadyInstalled, settings.Name));
            return 0;
        }

        var handler = handlers.FirstOrDefault(h =>
            string.Equals(h.Name, settings.Name, StringComparison.OrdinalIgnoreCase));

        if (handler is null)
        {
            console.MarkupLine(string.Format(SR.Current.ExtensionNotFound, settings.Name));
            return 1;
        }

        if (settings.Provider is not null &&
            handler.SupportedProviders.Count > 0 &&
            !handler.SupportedProviders.Any(p => string.Equals(p, settings.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            console.MarkupLine(string.Format(SR.Current.ExtensionInvalidProvider,
                settings.Provider, settings.Name, string.Join(", ", handler.SupportedProviders)));
            return 1;
        }

        IReadOnlyList<string> installedPackages = csprojPath is not null
            ? packageReader.ReadPackages(csprojPath)
            : [];

        var ctx = new ExtensionContext(csprojPath, projectDir, settings.Provider, installedPackages)
        {
            SolutionDir = solutionDir,
            RootNamespace = rootNamespace
        };

        var result = handler.Apply(ctx);
        if (!result.Success)
        {
            console.MarkupLine(string.Format(SR.Current.ExtensionApplyFailed, settings.Name, result.ErrorMessage));
            return 1;
        }

        registry.Register(projectDir, new ExtensionEntry(settings.Name, settings.Provider, DateTimeOffset.UtcNow));
        console.MarkupLine(string.Format(SR.Current.ExtensionAddSuccess, settings.Name));
        return 0;
    }
}
