using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBaseNetSqlServerCLI.Commands;


public class HelpSettings : CommandSettings
{
}

public class HelpCommand : AsyncCommand<HelpSettings>
{
    public override Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] HelpSettings settings, CancellationToken cancellationToken)
    {
        // Cabeçalho estilizado
        AnsiConsole.Write(new FigletText("OpenBaseNET").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]CLI de produtividade para Arquitetura Limpa e SQL Server[/]");
        AnsiConsole.WriteLine();

        // Tabela de Comandos
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Comando[/]");
        table.AddColumn("[bold yellow]Descrição[/]");
        table.AddColumn("[bold yellow]Exemplo de Uso[/]");

        table.AddRow(
            "[blue]install[/]",
            "Instala todos os pacotes de templates oficiais OpenBaseNET",
            "openbase [green]install[/]"
        );

        table.AddRow(
                    "[blue]new[/]",
                    "Cria um novo projeto estruturado",
                    "openbase new [green]--type api --template sqlserver --name ProjetoExemplo[/]"
                );

        table.AddRow("update", "Sincroniza e atualiza todos os Templates", "[blue]openbase update[/]");

        table.AddRow("version", "Mostra versões instaladas", "[blue]openbase version[/]");

        AnsiConsole.Write(table);

        // Painel de Dica Adicional
        var panel = new Panel(
            new Rows(
                new Markup("[bold white]Dica:[/] Utilize o sufixo [blue]--help[/] em qualquer comando para ver detalhes técnicos."),
                new Markup("[bold white]Repo:[/] [link]https://github.com/britors/OpenBaseNET.CLI[/]"),
                new Markup("[bold white]Email:[/] [link]mailto:rodrigo@w3ti.com.br[/]")
            )
        );
        panel.Header = new PanelHeader(" Suporte ");
        panel.Border = BoxBorder.Rounded;
        panel.Padding = new Padding(1, 1, 1, 1);

        AnsiConsole.Write(panel);

        return Task.FromResult(0);
    }
}