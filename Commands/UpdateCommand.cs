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
        };

        AnsiConsole.MarkupLine("[blue]Sincronizando templates OpenBase...[/]");

        var hasError = false;

        foreach (var packageId in officialPackages)
        {
            var exitCode = 0;

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
                        exitCode = process.ExitCode; // CORRIGIDO: exit code era ignorado
                    }
                });

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao atualizar [yellow]{packageId}[/].");
                hasError = true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {packageId} atualizado.");
            }
        }

        AnsiConsole.WriteLine();
        var cliExitCode = 0;

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
                    cliExitCode = tool.ExitCode;
                }
            });

        if (cliExitCode != 0)
        {
            AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao atualizar a OpenBase CLI.");
            hasError = true;
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✓[/] OpenBase CLI atualizada.");
        }

        AnsiConsole.WriteLine();

        if (hasError)
        {
            AnsiConsole.MarkupLine("[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Sucesso:[/] Todos os componentes estão na versão mais recente!");
        return 0;
    }
}