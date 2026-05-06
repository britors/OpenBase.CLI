using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class InstallSettings : CommandSettings
{
}

public class InstallCommand : AsyncCommand<InstallSettings>
{
    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] InstallSettings settings,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Iniciando a instalação dos pacotes OpenBase...[/]");

        var failed = false;

        foreach (var packageId in Helpers.DotNet.TemplatePackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Instalando {packageId}...", async _ =>
                {
                    var (success, error) = await Helpers.DotNet.RunAsync($"new install {packageId}", cancellationToken);
                    if (!success)
                    {
                        failed = true;
                        AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao instalar [yellow]{packageId}[/].");
                        if (!string.IsNullOrWhiteSpace(error))
                            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] {packageId} instalado.");
                    }
                });
        }

        return failed ? 1 : 0;
    }
}
