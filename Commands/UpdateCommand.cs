using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBaseSqlServerCLI.Commands;

public class UpdateSettings : CommandSettings
{
}
public class UpdateCommand : AsyncCommand<UpdateSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] UpdateSettings settings, CancellationToken cancellationToken)
    {
        // 1. Lista de pacotes oficiais que compõem o ecossistema OpenBase
        var officialPackages = new[]
        {
            "w3ti.OpenBase.SQLServer.Template",
        };

        AnsiConsole.MarkupLine("[blue]Sincronizando ecossistema OpenBase...[/]");

        foreach (var packageId in officialPackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Verificando/Atualizando {packageId}...", async ctx =>
                {
                    // O comando 'install' do dotnet já lida com atualização 
                    // se o pacote já existir, ou instalação se for novo.

                    var psi = new ProcessStartInfo(Helpers.DotNet.GetDotnetPath(), $"new install {packageId}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };

                    var process = Process.Start(psi);
                    if (process != null) await process.WaitForExitAsync(cancellationToken);
                });
        }

        AnsiConsole.MarkupLine("\n[green]Sucesso:[/] Todos os templates estão sincronizados e atualizados!");

        // 2. Atualizar a própria CLI (Auto-update sugerido)
        var psiTool = new ProcessStartInfo(Helpers.DotNet.GetDotnetPath(), "tool update -g w3ti.OpenBase.CLI")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        var tool = Process.Start(psiTool);
        if (tool != null) await tool.WaitForExitAsync(cancellationToken);

        AnsiConsole.MarkupLine("\n[green]Sucesso:[/] OpenBase CLI está atualizada!");
        return 0;
    }
}