using System.Diagnostics.CodeAnalysis;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class FileWriter : IFileWriter
{
    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
    public bool FileExists(string path) => File.Exists(path);
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
}
