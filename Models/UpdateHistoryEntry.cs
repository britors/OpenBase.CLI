namespace OpenBase.CLI.Models;

public sealed record UpdateHistoryEntry
{
    public DateTime Date { get; init; }
    public string? Component { get; init; }
    public string? PreviousVersion { get; init; }
    public string? NewVersion { get; init; }
    public bool Success { get; init; }
}
