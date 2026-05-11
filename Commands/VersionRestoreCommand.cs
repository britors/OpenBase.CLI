using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class VersionRestoreSettings : CommandSettings
{
    [CommandArgument(0, "<version>")]
    [Description("Versão para restaurar (ex: 10.5.9)")]
    public string Version { get; set; } = string.Empty;

    [CommandOption("--type")]
    [Description("Componente a restaurar: cli, sqlserver, postgres")]
    public string? Type { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Type))
            return ValidationResult.Error("Use --type para especificar o componente: cli, sqlserver, postgres");

        if (!ValidTypes.Contains(Type, StringComparer.OrdinalIgnoreCase))
            return ValidationResult.Error($"Tipo inválido '{Type}'. Use: cli, sqlserver, postgres");

        return ValidationResult.Success();
    }

    internal static readonly string[] ValidTypes = ["cli", "sqlserver", "postgres"];
}

public class VersionRestoreCommand : AsyncCommand<VersionRestoreSettings>
{
    private static readonly Dictionary<string, string> TypeToPackage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cli"] = "w3ti.OpenBase.CLI",
        ["sqlserver"] = "w3ti.OpenBaseNET.SQLServer.Template",
        ["postgres"] = "w3ti.OpenBaseNET.Postgres.Template",
    };

    private static readonly Dictionary<string, string> PackageDisplayName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["w3ti.OpenBase.CLI"] = "OpenBase CLI",
        ["w3ti.OpenBaseNET.SQLServer.Template"] = "Template SQLServer",
        ["w3ti.OpenBaseNET.Postgres.Template"] = "Template Postgres",
    };

    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;

    public VersionRestoreCommand(IDotNetRunner dotNetRunner, IAnsiConsole console)
    {
        _dotNetRunner = dotNetRunner;
        _console = console;
    }

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] VersionRestoreSettings settings,
        CancellationToken cancellationToken)
    {
        var packageId = TypeToPackage[settings.Type!];
        var displayName = PackageDisplayName[packageId];
        var version = settings.Version;

        _console.MarkupLine($"[blue]Restaurando {Markup.Escape(displayName)} para a versão {Markup.Escape(version)}...[/]");

        var arguments = packageId == "w3ti.OpenBase.CLI"
            ? $"tool update -g {packageId} --version {version}"
            : $"new install {packageId}::{version}";

        var succeeded = false;

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Aplicando versão {Markup.Escape(version)}...", async _ =>
            {
                var (ok, error) = await _dotNetRunner.RunAsync(arguments, cancellationToken);
                succeeded = ok;

                if (!ok)
                {
                    _console.MarkupLine($"[red]Erro:[/] Falha ao restaurar {Markup.Escape(displayName)} para a versão {Markup.Escape(version)}.");
                    if (!string.IsNullOrWhiteSpace(error))
                        _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                }
                else
                {
                    _console.MarkupLine($"[green]✓[/] {Markup.Escape(displayName)} restaurado para a versão {Markup.Escape(version)}.");
                }
            });

        return succeeded ? 0 : 1;
    }
}
