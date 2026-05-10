namespace OpenBase.CLI.Helpers;

public interface IProjectLocator
{
    (string? SolutionDir, string? RootNamespace) Detect(string workingDir, string? overrideNamespace);
}
