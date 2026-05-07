using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class UpdateSettings : CommandSettings
{
}

public class UpdateCommand : AsyncCommand<UpdateSettings>
{
    private readonly ITemplatePackageRunner _packageRunner;
    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;

    public UpdateCommand(ITemplatePackageRunner packageRunner, IDotNetRunner dotNetRunner, IAnsiConsole console)
    {
        _packageRunner = packageRunner;
        _dotNetRunner = dotNetRunner;
        _console = console;
    }

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] UpdateSettings settings,
        CancellationToken cancellationToken)
    {
        _console.MarkupLine("[blue]Sincronizando templates OpenBase...[/]");

        var failed = await _packageRunner.RunPackagesAsync(
            "Atualizando", "atualizado", "atualizar", cancellationToken);

        _console.WriteLine();

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Atualizando OpenBase CLI...", async _ =>
            {
                var (success, error) = await _dotNetRunner.RunAsync("tool update -g w3ti.OpenBase.CLI", cancellationToken);
                if (!success)
                {
                    failed = true;
                    _console.MarkupLine("[red]Erro:[/] Falha ao atualizar a OpenBase CLI.");
                    if (!string.IsNullOrWhiteSpace(error))
                        _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    _console.MarkupLine("[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.");
                }
                else
                {
                    _console.MarkupLine("[green]✓[/] OpenBase CLI atualizada.");
                }
            });

        return failed ? 1 : 0;
    }
}
