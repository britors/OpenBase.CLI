using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class VersionRestoreSettings : CommandSettings
{
    [CommandArgument(0, "<version>")]
    public string Version { get; set; } = "";
    [CommandOption("--type <COMPONENT>")]
    public string Type { get; set; } = "template";
    [CommandOption("--json")]
    public bool Json { get; set; }
}

public class VersionRestoreCommand(PackageOperations operations) : AsyncCommand<VersionRestoreSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, VersionRestoreSettings settings, CancellationToken token)
        => operations.RunAsync("version.restore", new InstallSettings { Type = settings.Type, Version = settings.Version, Json = settings.Json }, token);
}
