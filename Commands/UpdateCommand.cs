using System.Diagnostics;
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
        var officialPackages = new[]
        {
            "w3ti.OpenBaseNET.SQLServer.Template",
            "w3ti.OpenBaseNET.Postgres.Template",
        };

        AnsiConsole.MarkupLine("[blue]Sincronizando templates OpenBase...[/]");

        foreach (var packageId in officialPackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Atualizando {packageId}...", async ctx =>
                {
                    var psi = new ProcessStartInfo(Helpers.DotNet.GetDotnetPath(), $"new install {packageId}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        AnsiConsole.MarkupLine(process.ExitCode != 0
                            ? $"[red]Erro:[/] Falha ao atualizar [yellow]{packageId}[/]."
                            : $"[green]✓[/] {packageId} atualizado.");
                    }
                });

        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Atualizando OpenBase CLI...", async ctx =>
            {
                var psiTool = new ProcessStartInfo(
                    Helpers.DotNet.GetDotnetPath(),
                    "tool update -g w3ti.OpenBase.CLI")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var tool = Process.Start(psiTool);
                if (tool != null)
                {
                    await tool.WaitForExitAsync(cancellationToken);
                    if (tool.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao atualizar a OpenBase CLI.");
                        AnsiConsole.MarkupLine("[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] OpenBase CLI atualizada.");
                    }
                }
            });

        return 0;
    }
}