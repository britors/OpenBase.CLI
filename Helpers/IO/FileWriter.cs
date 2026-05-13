using System.Diagnostics.CodeAnalysis;

namespace OpenBase.CLI.Helpers.IO;

[ExcludeFromCodeCoverage]
public sealed class FileWriter : IFileWriter
{
    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
    public string? FindSolutionFile(string dir) =>
        Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();

    public string? FindFile(string rootDirectory, string fileName) =>
        Directory.Exists(rootDirectory)
            ? Directory.GetFiles(rootDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault()
            : null;
}
