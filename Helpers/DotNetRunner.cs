using System.Diagnostics.CodeAnalysis;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class DotNetRunner : IDotNetRunner
{
    public Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken)
        => DotNet.RunAsync(arguments, cancellationToken);

    public (bool Success, string Error) Run(string arguments)
        => DotNet.RunAsync(arguments, CancellationToken.None).GetAwaiter().GetResult();

    public string GetDotnetVersion() => DotNet.GetDotnetVersion();

    public bool IsSdkVersionSufficient(int requiredMajor) => DotNet.IsSdkVersionSufficient(requiredMajor);
}
