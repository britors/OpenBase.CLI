using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.Commands;

public class VersionSettings : CommandSettings
{
}


public class VersionCommand : AsyncCommand<VersionSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] VersionSettings settings, CancellationToken cancellationToken)
    {
        // Coleta de dados
        var dotnetVersion = Helpers.DotNet.GetDotnetVersion();
        var toolVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "--";
        var angular = Helpers.Angular.GetAngularVersion();
        
        // Informações do Sistema Operacional e Arquitetura
        var osDescription = RuntimeInformation.OSDescription; // Ex: "Fedora Linux 40", "Windows 11"
        var architecture = RuntimeInformation.OSArchitecture.ToString().ToLower(); // Ex: "x64", "arm64"

        AnsiConsole.Write(new FigletText("OpenBase").Color(Color.Blue));

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Componente[/]");
        table.AddColumn("[bold]Versão / Detalhes[/]");

        table.AddRow("OS", $"[green]{osDescription} ({architecture})[/]");
        table.AddRow("DotNet", $"[green]{dotnetVersion}[/]");
        table.AddRow("OpenBase CLI", $"[green]{toolVersion}[/]");
        table.AddRow("Angular CLI", $"[green]{angular}[/]");
        
        AnsiConsole.Write(table);
        
        return 0;
    }
}