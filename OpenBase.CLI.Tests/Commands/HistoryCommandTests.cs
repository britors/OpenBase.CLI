using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class HistoryCommandTests
{
    private readonly Mock<IUpdateHistoryService> _historyService = new();

    private HistoryCommand CreateCommand() =>
        new(_historyService.Object, CommandTestHelper.CreateConsole());

    private void SetupHistory(params UpdateHistoryEntry[] entries) =>
        _historyService
            .Setup(s => s.GetHistoryAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

    private static UpdateHistoryEntry MakeEntry(string component = "w3ti.OpenBase.CLI") =>
        new()
        {
            Date = DateTime.UtcNow,
            Component = component,
            PreviousVersion = "1.0.0",
            NewVersion = "1.0.1",
            Success = true
        };

    [Fact]
    public async Task ExecuteAsync_NoEntries_ReturnsZero()
    {
        SetupHistory();

        var result = await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithEntries_ReturnsZero()
    {
        SetupHistory(MakeEntry());

        var result = await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidType_ReturnsOne()
    {
        var result = await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings { Type = "invalido" }, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("cli", "w3ti.OpenBase.CLI")]
    [InlineData("CLI", "w3ti.OpenBase.CLI")]
    [InlineData("sqlserver", "w3ti.OpenBaseNET.SQLServer.Template")]
    [InlineData("SQLServer", "w3ti.OpenBaseNET.SQLServer.Template")]
    [InlineData("postgres", "w3ti.OpenBaseNET.Postgres.Template")]
    [InlineData("POSTGRES", "w3ti.OpenBaseNET.Postgres.Template")]
    public async Task ExecuteAsync_ValidType_QueriesCorrectComponent(string type, string expectedComponent)
    {
        _historyService
            .Setup(s => s.GetHistoryAsync(expectedComponent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UpdateHistoryEntry>());

        await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings { Type = type }, CancellationToken.None);

        _historyService.Verify(s => s.GetHistoryAsync(expectedComponent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoType_QueriesWithNullComponent()
    {
        SetupHistory();

        await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings(), CancellationToken.None);

        _historyService.Verify(s => s.GetHistoryAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidType_DoesNotQueryHistory()
    {
        await ((ICommand<HistorySettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("history"), new HistorySettings { Type = "invalido" }, CancellationToken.None);

        _historyService.Verify(s => s.GetHistoryAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
