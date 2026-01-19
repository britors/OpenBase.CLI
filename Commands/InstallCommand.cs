using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
                        var psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"new install {packageId}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // Caminhos padrão e seguros para sistemas Unix/Linux
                            psi.Environment["PATH"] = "/usr/bin:/usr/local/bin:/bin:/usr/share/dotnet";
                        }

                        using var process = Process.Start(psi);
                        if (process != null)
                        {
                            await process.WaitForExitAsync(cancellationToken);
                        }
                    });
        }

        AnsiConsole.MarkupLine("[green]Sucesso:[/] Todos os templates foram instalados e estão prontos para uso!");
        return 0;
    }
}