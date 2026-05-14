namespace OpenBase.CLI.Helpers.IO;

public interface ICsprojPackageReader
{
    IReadOnlyList<string> ReadPackages(string csprojPath);
}
