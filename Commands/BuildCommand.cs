using System.ComponentModel;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class BuildSettings : CommandSettings
{
    [CommandOption("-c|--configuration <configuration>")]
    [Description("Build configuration (Debug or Release)")]
    [DefaultValue("Debug")]
    public string Configuration { get; set; } = "Debug";

    [CommandOption("--no-restore")]
    [Description("Skip dotnet restore")]
    public bool NoRestore { get; set; }
}

public class BuildCommand(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IProjectLocator projectLocator,
    ICsprojLocator csprojLocator,
    IFileWriter fileWriter) : AsyncCommand<BuildSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, BuildSettings settings, CancellationToken cancellationToken)
    {
        var target = FindTarget();
        if (target is null)
        {
            console.MarkupLine(SR.Current.BuildNoProjectFound);
            return 1;
        }

        if (!settings.NoRestore && !await RunStepAsync(
                SR.Current.BuildRestoring,
                $"restore \"{target}\"",
                cancellationToken))
            return 1;

        if (!await RunStepAsync(
                SR.Current.BuildBuilding,
                $"build \"{target}\" --configuration {settings.Configuration} --no-restore",
                cancellationToken))
            return 1;

        if (!await RunStepAsync(
                SR.Current.BuildTesting,
                $"test \"{target}\" --configuration {settings.Configuration} --no-build",
                cancellationToken))
            return 1;

        console.MarkupLine(SR.Current.BuildSuccess);
        return 0;
    }

    private string? FindTarget()
    {
        var workingDir = Directory.GetCurrentDirectory();
        var (solutionDir, _) = projectLocator.Detect(workingDir, null);

        if (solutionDir is not null)
        {
            var slnFile = fileWriter.FindSolutionFile(solutionDir);
            if (slnFile is not null) return slnFile;
        }

        return csprojLocator.Find(workingDir);
    }

    private async Task<bool> RunStepAsync(string label, string args, CancellationToken cancellationToken)
    {
        console.MarkupLine(label);
        var (ok, err) = await dotNetRunner.RunAsync(args, cancellationToken);

        if (ok)
            console.MarkupLine(SR.Current.BuildStepSuccess);
        else
        {
            console.MarkupLine(SR.Current.BuildStepFailed);
            if (!string.IsNullOrWhiteSpace(err))
                console.MarkupLine($"[grey]{Markup.Escape(err)}[/]");
            console.MarkupLine(SR.Current.BuildFailed);
        }

        return ok;
    }
}
