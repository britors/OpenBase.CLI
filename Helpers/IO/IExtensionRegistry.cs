using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.IO;

public interface IExtensionRegistry
{
    IReadOnlyList<ExtensionEntry> GetAll(string projectDir);
    bool IsInstalled(string projectDir, string name, string? provider);
    void Register(string projectDir, ExtensionEntry entry);
}
