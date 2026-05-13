using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.IO;

public interface IUpdateHistoryService
{
    Task AddEntryAsync(UpdateHistoryEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<UpdateHistoryEntry>> GetHistoryAsync(string? component, CancellationToken cancellationToken);
}
