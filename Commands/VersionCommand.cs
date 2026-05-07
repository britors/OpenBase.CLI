using System.Reflection;
using System.Runtime.InteropServices;
using OpenBase.CLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class VersionSettings : CommandSettings
{
}

public class VersionCommand(IDotNetRunner dotNetRunner) : Command<VersionSettings>
{
    private const string Codename = "Andromeda";

    protected override int Execute(CommandContext context, VersionSettings settings, CancellationToken cancellationToken)
    {
        var dotnetVersion = dotNetRunner.GetDotnetVersion();
        var toolVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "--";
        var osDescription = RuntimeInformation.OSDescription;
        var architecture = RuntimeInformation.OSArchitecture.ToString().ToLower();

        AnsiConsole.Write(new FigletText("OpenBase").Color(Color.Blue));

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Componente[/]");
        table.AddColumn("[bold]Versão / Detalhes[/]");

        table.AddRow("OS", $"[green]{osDescription} ({architecture})[/]");
        table.AddRow("DotNet", $"[green]{dotnetVersion}[/]");
        table.AddRow("OpenBase CLI", $"[green]{toolVersion} \"{Codename}\"[/]");

        AnsiConsole.Write(table);

        return 0;
    }
}
