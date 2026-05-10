namespace OpenBase.CLI.Helpers;

public interface IFileWriter
{
    void EnsureDirectory(string path);
    bool FileExists(string path);
    void WriteAllText(string path, string content);
}
