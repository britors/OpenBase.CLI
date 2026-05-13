using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class VersionSettings : CommandSettings
{
}

public class VersionCommand(
    IDotNetRunner dotNetRunner,
    IAnsiConsole console) : AsyncCommand<VersionSettings>
{
    private const string Codename = "Andromeda";

    private static readonly (string Component, string Label)[] TrackedComponents =
    [
        ("w3ti.OpenBase.CLI",                     "OpenBase CLI"),
        ("w3ti.OpenBaseNET.SQLServer.Template",   "Template SQLServer"),
        ("w3ti.OpenBaseNET.Postgres.Template",    "Template Postgres"),
    ];

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] VersionSettings settings,
        CancellationToken cancellationToken)
    {
        var dotnetVersion = dotNetRunner.GetDotnetVersion();
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "--";
        var osDescription = RuntimeInformation.OSDescription;
        var architecture = RuntimeInformation.OSArchitecture.ToString().ToLower();

        var installedVersions = new Dictionary<string, string?>();
        foreach (var (component, _) in TrackedComponents)
        {
            installedVersions[component] = component == "w3ti.OpenBase.CLI"
                ? await dotNetRunner.GetInstalledToolVersionAsync(component, cancellationToken)
                : await dotNetRunner.GetInstalledTemplateVersionAsync(component, cancellationToken);
        }

        console.Write(new FigletText("OpenBase").Color(Color.Blue));

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(SR.Current.ColVersionComponent);
        table.AddColumn(SR.Current.ColVersion);

        table.AddRow("OS", $"[green]{Markup.Escape(osDescription)} ({architecture})[/]");
        table.AddRow("DotNet", $"[green]{Markup.Escape(dotnetVersion)}[/]");

        foreach (var (component, label) in TrackedComponents)
        {
            var version = installedVersions[component];
            var isCli = component == "w3ti.OpenBase.CLI";

            string display;
            if (version != null)
                display = isCli
                    ? $"[green]{Markup.Escape(version)} \"{Codename}\"[/]"
                    : $"[green]{Markup.Escape(version)}[/]";
            else
                display = isCli
                    ? $"[yellow]{Markup.Escape(assemblyVersion)} \"{Codename}\"[/]"
                    : "[grey]--[/]";

            table.AddRow(label, display);
        }

        console.Write(table);

        return 0;
    }
}
