using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class UpdateSettings : InstallSettings { }
public class UpdateCommand(PackageOperations operations) : AsyncCommand<UpdateSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, UpdateSettings settings, CancellationToken token)
        => operations.RunAsync("update", settings, token);
}
