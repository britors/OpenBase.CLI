using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class InstallSettings : CommandSettings
{
}

public class InstallCommand : AsyncCommand<InstallSettings>
{
    private readonly ITemplatePackageRunner _runner;

    public InstallCommand(ITemplatePackageRunner runner)
    {
        _runner = runner;
    }

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] InstallSettings settings,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Iniciando a instalação dos pacotes OpenBase...[/]");

        var failed = await _runner.RunPackagesAsync(
            "Instalando", "instalado", "instalar", cancellationToken);

        return failed ? 1 : 0;
    }
}
