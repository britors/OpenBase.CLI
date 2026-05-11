using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class VersionCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IUpdateHistoryService> _historyService = new();

    private VersionCommand CreateCommand() =>
        new(_dotNetRunner.Object, _historyService.Object, CommandTestHelper.CreateConsole());

    private void SetupDotnet(string version = "10.0.0") =>
        _dotNetRunner.Setup(r => r.GetDotnetVersion()).Returns(version);

    private void SetupHistory(string component, string? newVersion) =>
        _historyService
            .Setup(s => s.GetHistoryAsync(component, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newVersion is null
                ? Array.Empty<UpdateHistoryEntry>()
                : [new UpdateHistoryEntry { Component = component, NewVersion = newVersion, Success = true, Date = DateTime.UtcNow }]);

    private void SetupNoHistory() =>
        _historyService
            .Setup(s => s.GetHistoryAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UpdateHistoryEntry>());

    [Fact]
    public async Task Execute_ReturnsZero()
    {
        SetupDotnet();
        SetupNoHistory();

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_CallsGetDotnetVersion()
    {
        SetupDotnet("10.0.1");
        SetupNoHistory();

        await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        _dotNetRunner.Verify(r => r.GetDotnetVersion(), Times.Once);
    }

    [Fact]
    public async Task Execute_UnknownDotnetVersion_ReturnsZero()
    {
        SetupDotnet("--");
        SetupNoHistory();

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_QueriesHistoryForAllTrackedComponents()
    {
        SetupDotnet();
        SetupNoHistory();

        await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        _historyService.Verify(s => s.GetHistoryAsync("w3ti.OpenBase.CLI", It.IsAny<CancellationToken>()), Times.Once);
        _historyService.Verify(s => s.GetHistoryAsync("w3ti.OpenBaseNET.SQLServer.Template", It.IsAny<CancellationToken>()), Times.Once);
        _historyService.Verify(s => s.GetHistoryAsync("w3ti.OpenBaseNET.Postgres.Template", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithHistory_ReturnsZero()
    {
        SetupDotnet();
        SetupHistory("w3ti.OpenBase.CLI", "10.5.11");
        SetupHistory("w3ti.OpenBaseNET.SQLServer.Template", "2.0.1");
        SetupHistory("w3ti.OpenBaseNET.Postgres.Template", "1.5.3");

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_FailedHistoryEntriesIgnored_FallsBackToNoVersion()
    {
        SetupDotnet();
        _historyService
            .Setup(s => s.GetHistoryAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UpdateHistoryEntry { NewVersion = "1.0.0", Success = false, Date = DateTime.UtcNow }]);

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }
}
