using System.Diagnostics;
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
        var packages = new[]
        {
            "w3ti.OpenBaseNET.SQLServer.Template",
        };

        AnsiConsole.MarkupLine("[blue]Iniciando a instalação dos pacotes OpenBase...[/]");

        var hasError = false;

        foreach (var packageId in packages)
        {

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Instalando {packageId}...", async ctx =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Helpers.DotNet.GetDotnetPath(),
                        Arguments = $"new install {packageId}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        hasError = (process.ExitCode != 0);
                    }
                });

            if (hasError)
            {
                AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao instalar [yellow]{packageId}[/].");
                AnsiConsole.MarkupLine("[red]Atenção:[/] Um ou mais pacotes falharam na instalação.");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] {packageId} instalado.");
        }


        AnsiConsole.MarkupLine("[green]Sucesso:[/] Todos os templates foram instalados e estão prontos para uso!");
        return 0;
    }
}