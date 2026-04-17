using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class HelpSettings : CommandSettings
{
}

public class HelpCommand : Command<HelpSettings>
{
    protected override int Execute(CommandContext context, HelpSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new FigletText("OpenBase").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]CLI de produtividade para Arquitetura Limpa[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Comando[/]");
        table.AddColumn("[bold yellow]Descrição[/]");
        table.AddColumn("[bold yellow]Exemplo de uso[/]");

        table.AddRow(
            "[blue]install[/]",
            "Instala todos os templates NuGet do OpenBase",
            "openbase [green]install[/]"
        );

        table.AddRow(
            "[blue]new[/]",
            "Cria um novo projeto estruturado",
            "openbase [green]new --type api --template sqlserver --name MeuProjeto[/]"
        );

        table.AddRow(
            "[blue]update[/]",
            "Sincroniza e atualiza templates e a CLI",
            "openbase [green]update[/]"
        );

        table.AddRow(
            "[blue]version[/]",
            "Mostra versões do ambiente instalado",
            "openbase [green]version[/]"
        );

        // CORRIGIDO: linhas "update" e "version" estavam sem markup azul, inconsistente com as demais
        AnsiConsole.Write(table);

        var panel = new Panel(
            new Rows(
                new Markup("[bold white]Dica:[/] Use [blue]--help[/] após qualquer comando para ver detalhes técnicos."),
                new Markup("[bold white]Repo:[/] [link]https://github.com/britors/OpenBase.CLI[/]"),
                new Markup("[bold white]Email:[/] [link]mailto:rodrigo@w3ti.com.br[/]")
            )
        )
        {
            Header = new PanelHeader(" Suporte "),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        };
        AnsiConsole.Write(panel);
        return 0;
    }
}