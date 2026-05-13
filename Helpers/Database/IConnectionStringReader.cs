namespace OpenBase.CLI.Helpers.Database;

public interface IConnectionStringReader
{
    string? Read(string solutionDir, string rootNamespace);
}
