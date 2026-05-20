using OpenBase.CLI.Helpers;
using OpenBase.CLI.Localization;
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
        ConsoleBanner.Print();
        AnsiConsole.MarkupLine(SR.Current.HelpSubtitle);
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn(SR.Current.HelpColCommand);
        table.AddColumn(SR.Current.HelpColDescription);
        table.AddColumn(SR.Current.HelpColExample);

        table.AddRow("[blue]build[/]", SR.Current.HelpBuildDesc, "openbase [green]build[/]\nopenbase [green]build --configuration Release[/]\nopenbase [green]build --no-restore[/]");
        table.AddRow("[blue]run[/]", SR.Current.HelpRunDesc, "openbase [green]run[/]\nopenbase [green]run --configuration Release[/]\nopenbase [green]run --no-build[/]");
        table.AddRow("[blue]install[/]", SR.Current.HelpInstallDesc, "openbase [green]install[/]");
        table.AddRow("[blue]new[/]", SR.Current.HelpNewDesc, "openbase [green]new --type api --template sqlserver --name MeuProjeto[/]\nopenbase [green]new --type api --template pgsql --name MeuProjeto[/]\nopenbase [green]new --type api --template oracle --name MeuProjeto[/]");
        table.AddRow("[blue]scaffold[/]", SR.Current.HelpScaffoldDesc, "openbase [green]scaffold --entity Produto[/]\nopenbase [green]scaffold --entity Produto --update[/]");
        table.AddRow("[blue]specialist[/]", SR.Current.HelpSpecialistDesc, "openbase [green]specialist --entity Produto[/]");
        table.AddRow("[blue]procedure[/]", SR.Current.HelpProcedureDesc, "openbase [green]procedure --name GetOrderById[/]\nopenbase [green]procedure --name GetOrderById --schema dbo[/]");
        table.AddRow("[blue]extension add[/]", SR.Current.HelpExtensionAddDesc, "openbase [green]extension add jwt[/]\nopenbase [green]extension add healthchecks[/]\nopenbase [green]extension add redis[/]");
        table.AddRow("[blue]extension list[/]", SR.Current.HelpExtensionListDesc, "openbase [green]extension list[/]");
        table.AddRow("[blue]history[/]", SR.Current.HelpHistoryDesc, "openbase [green]history[/]\nopenbase [green]history --type cli[/]");
        table.AddRow("[blue]history --clear[/]", SR.Current.HelpHistoryClearDesc, "openbase [green]history --clear[/]");
        table.AddRow("[blue]update[/]", SR.Current.HelpUpdateDesc, "openbase [green]update[/]");
        table.AddRow("[blue]version show[/]", SR.Current.HelpVersionShowDesc, "openbase [green]version show[/]");
        table.AddRow("[blue]version restore[/]", SR.Current.HelpVersionRestoreDesc, "openbase [green]version restore 10.5.9 --type cli[/]");

        AnsiConsole.Write(table);

        var panel = new Panel(
            new Rows(
                new Markup(SR.Current.HelpTip),
                new Markup("[bold white]Repo:[/] [link]https://github.com/britors/OpenBase.CLI[/]"),
                new Markup("[bold white]Email:[/] [link]mailto:rodrigo@w3ti.com.br[/]")
            )
        )
        {
            Header = new PanelHeader(SR.Current.HelpSupport),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        };
        AnsiConsole.Write(panel);
        return 0;
    }
}