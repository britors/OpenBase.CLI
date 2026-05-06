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
        AnsiConsole.MarkupLine("[blue]Iniciando a instalação dos pacotes OpenBase...[/]");

        var failed = false;

        foreach (var packageId in Helpers.DotNet.TemplatePackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Instalando {packageId}...", async _ =>
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
                        var errorOutput = process.StandardError.ReadToEndAsync(cancellationToken);
                        await process.WaitForExitAsync(cancellationToken);
                        if (process.ExitCode != 0)
                        {
                            failed = true;
                            var error = await errorOutput;
                            AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao instalar [yellow]{packageId}[/].");
                            if (!string.IsNullOrWhiteSpace(error))
                                AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error.Trim())}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] {packageId} instalado.");
                        }
                    }
                });
        }

        return failed ? 1 : 0;
    }
}
