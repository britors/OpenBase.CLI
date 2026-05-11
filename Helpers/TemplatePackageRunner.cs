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

    public async Task<IReadOnlyList<(string PackageId, bool Success)>> RunPackagesAsync(
        string statusVerb, string successLabel, string errorLabel, CancellationToken cancellationToken)
    {
        var results = new List<(string PackageId, bool Success)>();

        foreach (var packageId in DotNet.TemplatePackages)
        {
            var success = false;

            await _console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"{statusVerb} {packageId}...", async _ =>
                {
                    var (ok, error) = await _dotNetRunner.RunAsync($"new install {packageId}", cancellationToken);
                    success = ok;
                    if (!ok)
                    {
                        _console.MarkupLine($"[red]Erro:[/] Falha ao {errorLabel} [yellow]{packageId}[/].");
                        if (!string.IsNullOrWhiteSpace(error))
                            _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    }
                    else
                    {
                        _console.MarkupLine($"[green]✓[/] {packageId} {successLabel}.");
                    }
                });

            results.Add((packageId, success));
        }

        return results;
    }
}
