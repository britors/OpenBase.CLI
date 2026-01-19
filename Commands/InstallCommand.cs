using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBaseNetSqlServerCLI.Commands;


public class InstallSettings : CommandSettings
{
}


public class InstallCommand : AsyncCommand<InstallSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] InstallSettings settings, CancellationToken cancellationToken)
    {
        var packages = new[] 
        { 
            "w3ti.OpenBaseNET.SQLServer.Template",
            // Adicione aqui outros pacotes quando existirem
        };

        AnsiConsole.MarkupLine("[blue]Iniciando a instalação dos pacotes OpenBaseNET...[/]");

        foreach (var packageId in packages)
        {
            await AnsiConsole.Status()
                .StartAsync($"Instalando {packageId}...", async ctx =>
                {
                    var psi = new ProcessStartInfo("dotnet", $"new install {packageId}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    psi.EnvironmentVariables.Remove("PATH");
                    var process = Process.Start(psi);
                    if (process != null) await process.WaitForExitAsync(cancellationToken);
                });
        }

        AnsiConsole.MarkupLine("[green]Sucesso:[/] Todos os templates foram instalados e estão prontos para uso!");
        return 0;
    }
}