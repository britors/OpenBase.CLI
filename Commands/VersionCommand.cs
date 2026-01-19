using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBaseNetSqlServerCLI.Commands;

public class VersionSettings : CommandSettings
{
}

public class VersionCommand : AsyncCommand<VersionSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] VersionSettings settings, CancellationToken cancellationToken)
    {
        var toolVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "--";
        AnsiConsole.Write(new FigletText("OpenBaseNET").Color(Color.Blue));

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Componente[/]");
        table.AddColumn("[bold]Versão Instalada[/]");

        table.AddRow("OpenBase CLI", $"[green]{toolVersion}[/]");
    
        AnsiConsole.Write(table);
        return 0;
    }
}