using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Localization;
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
            return ValidationResult.Error(SR.Current.UseTypeToSpecify);

        if (!PackageIds.TypeToId.ContainsKey(Type))
            return ValidationResult.Error(string.Format(SR.Current.InvalidType, Type));

        return ValidationResult.Success();
    }

    internal static readonly string[] ValidTypes = ["cli", "sqlserver", "postgres"];
}

public class VersionRestoreCommand : AsyncCommand<VersionRestoreSettings>
{
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
        var packageId   = PackageIds.TypeToId[settings.Type!];
        var displayName = PackageIds.DisplayNames[packageId];
        var version     = settings.Version;

        _console.MarkupLine(string.Format(SR.Current.RestoringToVersion, Markup.Escape(displayName), Markup.Escape(version)));

        var arguments = packageId == PackageIds.Cli
            ? $"tool update -g {packageId} --version {version}"
            : $"new install {packageId}::{version}";

        var succeeded = false;

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(string.Format(SR.Current.ApplyingVersion, Markup.Escape(version)), async _ =>
            {
                var (ok, error) = await _dotNetRunner.RunAsync(arguments, cancellationToken);
                succeeded = ok;

                if (!ok)
                {
                    _console.MarkupLine(string.Format(SR.Current.RestoreFailed, Markup.Escape(displayName), Markup.Escape(version)));
                    if (!string.IsNullOrWhiteSpace(error))
                        _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                }
                else
                {
                    _console.MarkupLine(string.Format(SR.Current.RestoreSuccess, Markup.Escape(displayName), Markup.Escape(version)));
                }
            });

        return succeeded ? 0 : 1;
    }
}
