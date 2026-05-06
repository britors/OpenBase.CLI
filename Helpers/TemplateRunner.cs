using Spectre.Console;

namespace OpenBase.CLI.Helpers;

internal static class TemplateRunner
{
    internal static async Task<bool> RunPackagesAsync(
        string statusVerb, string successLabel, string errorLabel, CancellationToken cancellationToken)
    {
        var failed = false;

        foreach (var packageId in DotNet.TemplatePackages)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"{statusVerb} {packageId}...", async _ =>
                {
                    var (success, error) = await DotNet.RunAsync($"new install {packageId}", cancellationToken);
                    if (!success)
                    {
                        failed = true;
                        AnsiConsole.MarkupLine($"[red]Erro:[/] Falha ao {errorLabel} [yellow]{packageId}[/].");
                        if (!string.IsNullOrWhiteSpace(error))
                            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] {packageId} {successLabel}.");
                    }
                });
        }

        return failed;
    }
}
