namespace OpenBase.CLI.Helpers.IO;

public interface ICsprojLocator
{
    string? Find(string workingDir);
}
