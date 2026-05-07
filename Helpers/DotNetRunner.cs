using System.Diagnostics.CodeAnalysis;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class DotNetRunner : IDotNetRunner
{
    public Task<(bool Success, string Error)> RunAsync(string arguments, CancellationToken cancellationToken)
        => DotNet.RunAsync(arguments, cancellationToken);

    public string GetDotnetVersion() => DotNet.GetDotnetVersion();
}
