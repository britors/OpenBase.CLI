using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class UpdateSettings : CommandSettings
{
}

public class UpdateCommand : AsyncCommand<UpdateSettings>
{
    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] UpdateSettings settings,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Sincronizando templates OpenBase...[/]");

        var failed = await Helpers.TemplateRunner.RunPackagesAsync(
            "Atualizando", "atualizado", "atualizar", cancellationToken);

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Atualizando OpenBase CLI...", async _ =>
            {
                var (success, error) = await Helpers.DotNet.RunAsync("tool update -g w3ti.OpenBase.CLI", cancellationToken);
                if (!success)
                {
                    failed = true;
                    AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao atualizar a OpenBase CLI.");
                    if (!string.IsNullOrWhiteSpace(error))
                        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    AnsiConsole.MarkupLine("[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]✓[/] OpenBase CLI atualizada.");
                }
            });

        return failed ? 1 : 0;
    }
}
