namespace OpenBase.CLI.Helpers;

public interface IDotNetRunner
{
    Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken);
    string GetDotnetVersion();
}
