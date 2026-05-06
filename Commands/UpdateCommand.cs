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
        AnsiConsole.MarkupLine("[blue]Sincronizando templates OpenBase...[/]");

        var failed = false;

        foreach (var packageId in Helpers.DotNet.TemplatePackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Atualizando {packageId}...", async _ =>
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
                        var errorOutput = process.StandardError.ReadToEndAsync(cancellationToken);
                        await process.WaitForExitAsync(cancellationToken);
                        if (process.ExitCode != 0)
                        {
                            failed = true;
                            var error = await errorOutput;
                            AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao atualizar [yellow]{packageId}[/].");
                            if (!string.IsNullOrWhiteSpace(error))
                                AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error.Trim())}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] {packageId} atualizado.");
                        }
                    }
                });
        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Atualizando OpenBase CLI...", async _ =>
            {
                var psi = new ProcessStartInfo(
                    Helpers.DotNet.GetDotnetPath(),
                    "tool update -g w3ti.OpenBase.CLI")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var tool = Process.Start(psi);
                if (tool != null)
                {
                    var errorOutput = tool.StandardError.ReadToEndAsync(cancellationToken);
                    await tool.WaitForExitAsync(cancellationToken);
                    if (tool.ExitCode != 0)
                    {
                        failed = true;
                        var error = await errorOutput;
                        AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao atualizar a OpenBase CLI.");
                        if (!string.IsNullOrWhiteSpace(error))
                            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error.Trim())}[/]");
                        AnsiConsole.MarkupLine("[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] OpenBase CLI atualizada.");
                    }
                }
            });

        return failed ? 1 : 0;
    }
}
