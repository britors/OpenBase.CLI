namespace OpenBase.CLI.Helpers.Execution;

public interface IDotNetRunner
{
    Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken);
    (bool Success, string Error) Run(string arguments);
    string GetDotnetVersion();
    bool IsSdkVersionSufficient(int requiredMajor);
    Task<string?> GetInstalledToolVersionAsync(string packageId, CancellationToken cancellationToken);
    Task<string?> GetInstalledTemplateVersionAsync(string packageId, CancellationToken cancellationToken);
}
