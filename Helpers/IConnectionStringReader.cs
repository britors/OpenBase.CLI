namespace OpenBase.CLI.Helpers;

public interface IConnectionStringReader
{
    string? Read(string solutionDir, string rootNamespace);
}
