using System.Text.Json;
using OpenBase.CLI.Helpers;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Helpers;

public class UpdateHistoryServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _historyFile;

    public UpdateHistoryServiceTests()
    {
        _historyFile = Path.Combine(_tempDir, "update-history.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private UpdateHistoryService CreateService() => new(_historyFile);

    private static UpdateHistoryEntry MakeEntry(
        bool success = true,
        string? prev = "1.0.0",
        string? next = "1.0.1",
        string? component = "w3ti.OpenBase.CLI",
        DateTime? date = null) =>
        new()
        {
            Date = date ?? DateTime.UtcNow,
            Component = component,
            PreviousVersion = prev,
            NewVersion = next,
            Success = success
        };

    [Fact]
    public async Task AddEntryAsync_CreatesFileOnFirstRun()
    {
        await CreateService().AddEntryAsync(MakeEntry(), CancellationToken.None);

        Assert.True(File.Exists(_historyFile));
    }

    [Fact]
    public async Task AddEntryAsync_WritesValidJson()
    {
        await CreateService().AddEntryAsync(MakeEntry(next: "2.0.0"), CancellationToken.None);

        var json = await File.ReadAllTextAsync(_historyFile);
        var entries = JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json);

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("2.0.0", entries[0].NewVersion);
    }

    [Fact]
    public async Task AddEntryAsync_AppendsToExistingHistory()
    {
        var service = CreateService();
        await service.AddEntryAsync(MakeEntry(next: "1.0.1"), CancellationToken.None);
        await service.AddEntryAsync(MakeEntry(next: "1.0.2"), CancellationToken.None);

        var json = await File.ReadAllTextAsync(_historyFile);
        var entries = JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json);

        Assert.Equal(2, entries!.Count);
    }

    [Fact]
    public async Task AddEntryAsync_HandlesCorruptFileGracefully()
    {
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(_historyFile, "{ invalid json }");

        await CreateService().AddEntryAsync(MakeEntry(next: "3.0.0"), CancellationToken.None);

        var json = await File.ReadAllTextAsync(_historyFile);
        var entries = JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json);

        Assert.Single(entries!);
        Assert.Equal("3.0.0", entries![0].NewVersion);
    }

    [Fact]
    public async Task AddEntryAsync_SavesSuccessFlag()
    {
        await CreateService().AddEntryAsync(MakeEntry(success: false, next: null), CancellationToken.None);

        var json = await File.ReadAllTextAsync(_historyFile);
        var entries = JsonSerializer.Deserialize<List<UpdateHistoryEntry>>(json);

        Assert.False(entries![0].Success);
        Assert.Null(entries[0].NewVersion);
    }

    [Fact]
    public async Task GetHistoryAsync_NoComponent_ReturnsAllEntries()
    {
        var service = CreateService();
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBase.CLI"), CancellationToken.None);
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBaseNET.SQLServer.Template"), CancellationToken.None);

        var result = await service.GetHistoryAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_WithComponent_FiltersCorrectly()
    {
        var service = CreateService();
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBase.CLI"), CancellationToken.None);
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBaseNET.SQLServer.Template"), CancellationToken.None);

        var result = await service.GetHistoryAsync("w3ti.OpenBase.CLI", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("w3ti.OpenBase.CLI", result[0].Component);
    }

    [Fact]
    public async Task GetHistoryAsync_WithComponent_CaseInsensitive()
    {
        var service = CreateService();
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBase.CLI"), CancellationToken.None);

        var result = await service.GetHistoryAsync("W3TI.OPENBASE.CLI", CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEntriesNewestFirst()
    {
        var service = CreateService();
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);

        await service.AddEntryAsync(MakeEntry(next: "1.0.0", date: older), CancellationToken.None);
        await service.AddEntryAsync(MakeEntry(next: "2.0.0", date: newer), CancellationToken.None);

        var result = await service.GetHistoryAsync(null, CancellationToken.None);

        Assert.Equal("2.0.0", result[0].NewVersion);
        Assert.Equal("1.0.0", result[1].NewVersion);
    }

    [Fact]
    public async Task GetHistoryAsync_NoFile_ReturnsEmpty()
    {
        var result = await CreateService().GetHistoryAsync(null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ComponentNotFound_ReturnsEmpty()
    {
        var service = CreateService();
        await service.AddEntryAsync(MakeEntry(component: "w3ti.OpenBase.CLI"), CancellationToken.None);

        var result = await service.GetHistoryAsync("w3ti.OpenBaseNET.SQLServer.Template", CancellationToken.None);

        Assert.Empty(result);
    }
}
