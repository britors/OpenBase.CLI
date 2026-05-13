namespace OpenBase.CLI.Helpers.IO;

public interface IProjectLocator
{
    (string? SolutionDir, string? RootNamespace) Detect(string workingDir, string? overrideNamespace);
}
