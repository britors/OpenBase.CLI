namespace OpenBase.CLI.Helpers.IO;

public interface IFileWriter
{
    void EnsureDirectory(string path);
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    string? FindSolutionFile(string dir);
    string? FindFile(string rootDirectory, string fileName);
}
