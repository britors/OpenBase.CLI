using System.Text.Json;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.IO;

public sealed class ExtensionRegistry : IExtensionRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string RegistryPath(string projectDir) =>
        Path.Combine(projectDir, ".openbase", "extensions.json");

    public IReadOnlyList<ExtensionEntry> GetAll(string projectDir)
    {
        var path = RegistryPath(projectDir);
        if (!File.Exists(path)) return [];
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<ExtensionEntry>>(json, JsonOptions) ?? [];
    }

    public bool IsInstalled(string projectDir, string name, string? provider) =>
        GetAll(projectDir).Any(e =>
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (provider is null || string.Equals(e.Provider, provider, StringComparison.OrdinalIgnoreCase)));

    public void Register(string projectDir, ExtensionEntry entry)
    {
        var path = RegistryPath(projectDir);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var entries = GetAll(projectDir).ToList();
        entries.Add(entry);
        File.WriteAllText(path, JsonSerializer.Serialize(entries, JsonOptions));
    }
}
