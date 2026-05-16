using System.ComponentModel;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class RunSettings : CommandSettings
{
    [CommandOption("-c|--configuration <configuration>")]
    [Description("Build configuration (Debug or Release)")]
    [DefaultValue("Debug")]
    public string Configuration { get; set; } = "Debug";

    [CommandOption("--no-build")]
    [Description("Skip build step")]
    public bool NoBuild { get; set; }
}

public class RunCommand(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IProjectLocator projectLocator,
    ICsprojLocator csprojLocator,
    IFileWriter fileWriter,
    IBrowserLauncher browserLauncher) : AsyncCommand<RunSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        var (solutionDir, ns) = projectLocator.Detect(Directory.GetCurrentDirectory(), null);

        if (solutionDir is null || ns is null)
        {
            console.MarkupLine(SR.Current.RunNoProjectFound);
            return 1;
        }

        var presentationCsproj = Path.Combine(
            solutionDir, "src", $"{ns}.Presentation.Api", $"{ns}.Presentation.Api.csproj");

        if (!fileWriter.FileExists(presentationCsproj))
        {
            console.MarkupLine(SR.Current.RunNoProjectFound);
            return 1;
        }

        if (!settings.NoBuild)
        {
            var slnFile = fileWriter.FindSolutionFile(solutionDir) ?? csprojLocator.Find(Directory.GetCurrentDirectory());
            if (slnFile is null)
            {
                console.MarkupLine(SR.Current.RunNoProjectFound);
                return 1;
            }

            if (!await BuildAsync(slnFile, settings.Configuration, cancellationToken))
                return 1;
        }

        var swaggerUrl = ResolveSwaggerUrl(solutionDir, ns) ?? $"https://localhost:5001/swagger";

        console.MarkupLine(string.Format(SR.Current.RunStarting, $"{ns}.Presentation.Api"));
        console.MarkupLine(string.Format(SR.Current.RunSwaggerUrl, swaggerUrl));

        _ = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)
            .ContinueWith(_ =>
            {
                console.MarkupLine(SR.Current.RunOpeningBrowser);
                browserLauncher.Open(swaggerUrl);
            }, TaskContinuationOptions.NotOnCanceled);

        var exitCode = await dotNetRunner.RunLiveAsync(
            $"run --project \"{presentationCsproj}\" --configuration {settings.Configuration}",
            cancellationToken);

        console.MarkupLine(SR.Current.RunStopped);
        return exitCode;
    }

    private async Task<bool> BuildAsync(string target, string configuration, CancellationToken cancellationToken)
    {
        console.MarkupLine(SR.Current.BuildRestoring);
        var (restoreOk, _) = await dotNetRunner.RunAsync($"restore \"{target}\"", cancellationToken);
        if (!restoreOk)
        {
            console.MarkupLine(SR.Current.BuildStepFailed);
            console.MarkupLine(SR.Current.BuildFailed);
            return false;
        }
        console.MarkupLine(SR.Current.BuildStepSuccess);

        console.MarkupLine(SR.Current.BuildBuilding);
        var (buildOk, buildErr) = await dotNetRunner.RunAsync(
            $"build \"{target}\" --configuration {configuration} --no-restore", cancellationToken);
        if (!buildOk)
        {
            console.MarkupLine(SR.Current.BuildStepFailed);
            if (!string.IsNullOrWhiteSpace(buildErr))
                console.MarkupLine($"[grey]{Markup.Escape(buildErr)}[/]");
            console.MarkupLine(SR.Current.BuildFailed);
            return false;
        }
        console.MarkupLine(SR.Current.BuildStepSuccess);

        return true;
    }

    public string? ResolveSwaggerUrl(string solutionDir, string ns)
    {
        var path = Path.Combine(
            solutionDir, "src", $"{ns}.Presentation.Api", "Properties", "launchSettings.json");

        if (!fileWriter.FileExists(path)) return null;

        try
        {
            var json = fileWriter.ReadAllText(path);
            var profiles = JsonNode.Parse(json)?["profiles"]?.AsObject();
            if (profiles is null) return null;

            foreach (var profile in profiles)
            {
                var appUrl = profile.Value?["applicationUrl"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(appUrl)) continue;

                var urls = appUrl.Split(';');
                var url = urls.FirstOrDefault(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                          ?? urls.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(url))
                    return $"{url.TrimEnd('/')}/swagger";
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }
}
