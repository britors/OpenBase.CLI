using Spectre.Console;

namespace OpenBase.CLI.Helpers;

public sealed class TemplatePackageRunner : ITemplatePackageRunner
{
    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;

    public TemplatePackageRunner(IDotNetRunner dotNetRunner, IAnsiConsole console)
    {
        _dotNetRunner = dotNetRunner;
        _console = console;
    }

    public async Task<bool> RunPackagesAsync(
        string statusVerb, string successLabel, string errorLabel, CancellationToken cancellationToken)
    {
        var failed = false;

        foreach (var packageId in DotNet.TemplatePackages)
        {
            await _console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"{statusVerb} {packageId}...", async _ =>
                {
                    var (success, error) = await _dotNetRunner.RunAsync($"new install {packageId}", cancellationToken);
                    if (!success)
                    {
                        failed = true;
                        _console.MarkupLine($"[red]Erro:[/] Falha ao {errorLabel} [yellow]{packageId}[/].");
                        if (!string.IsNullOrWhiteSpace(error))
                            _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    }
                    else
                    {
                        _console.MarkupLine($"[green]✓[/] {packageId} {successLabel}.");
                    }
                });
        }

        return failed;
    }
}
