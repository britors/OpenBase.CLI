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

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        VersionSettings settings,
        CancellationToken cancellationToken)
    {
        var dotnetVersion  = dotNetRunner.GetDotnetVersion();
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "--";
        var osDescription  = RuntimeInformation.OSDescription;
        var architecture   = RuntimeInformation.OSArchitecture.ToString().ToLower();

        var installedVersions = new Dictionary<string, string?>();
        foreach (var id in PackageIds.All)
        {
            installedVersions[id] = id == PackageIds.Cli
                ? await dotNetRunner.GetInstalledToolVersionAsync(id, cancellationToken)
                : await dotNetRunner.GetInstalledTemplateVersionAsync(id, cancellationToken);
        }

        console.Write(new FigletText("OpenBase").Color(Color.Blue));

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn(SR.Current.ColVersionComponent);
        table.AddColumn(SR.Current.ColVersion);

        table.AddRow("OS",     $"[green]{Markup.Escape(osDescription)} ({architecture})[/]");
        table.AddRow("DotNet", $"[green]{Markup.Escape(dotnetVersion)}[/]");

        foreach (var id in PackageIds.All)
            table.AddRow(PackageIds.DisplayNames[id], FormatVersionDisplay(id, installedVersions[id], assemblyVersion));

        console.Write(table);
        return 0;
    }

    private static string FormatVersionDisplay(string id, string? version, string assemblyVersion) =>
        (version, id == PackageIds.Cli) switch
        {
            (not null, true)  => $"[green]{Markup.Escape(version)} \"{Codename}\"[/]",
            (not null, false) => $"[green]{Markup.Escape(version)}[/]",
            (null,     true)  => $"[yellow]{Markup.Escape(assemblyVersion)} \"{Codename}\"[/]",
            _                 => "[grey]--[/]"
        };
}
