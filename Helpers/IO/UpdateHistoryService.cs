using System.Text.Json;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.IO;

public sealed class UpdateHistoryService : IUpdateHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _historyFilePath;

    public UpdateHistoryService(string? historyFilePath = null)
    {
        _historyFilePath = historyFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".openbase",
            "update-history.json");
    }

    public async Task AddEntryAsync(UpdateHistoryEntry entry, CancellationToken cancellationToken)
    {
        var history = await ReadAllAsync(cancellationToken);
        history.Add(entry);
        await WriteAllAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyList<UpdateHistoryEntry>> GetHistoryAsync(string? component, CancellationToken cancellationToken)
    {
        var history = await ReadAllAsync(cancellationToken);

        IEnumerable<UpdateHistoryEntry> filtered = history;
        if (!string.IsNullOrWhiteSpace(component))
            filtered = history.Where(e => string.Equals(e.Component, component, StringComparison.OrdinalIgnoreCase));

        return [.. filtered.OrderByDescending(e => e.Date)];
    }

    private async Task<List<UpdateHistoryEntry>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_historyFilePath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(_historyFilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task WriteAllAsync(List<UpdateHistoryEntry> history, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_historyFilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(history, JsonOptions);
        await File.WriteAllTextAsync(_historyFilePath, json, cancellationToken);
    }
}
