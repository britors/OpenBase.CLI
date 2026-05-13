using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class UpdateCommandTests
{
    private readonly Mock<ITemplatePackageRunner> _packageRunner = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IUpdateHistoryService> _historyService = new();

    private UpdateCommand CreateCommand() =>
        new(_packageRunner.Object, _dotNetRunner.Object, CommandTestHelper.CreateConsole(), _historyService.Object);

    private void SetupPackages(bool allSucceed) =>
        _packageRunner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DotNet.TemplatePackages.Select(p => (p, allSucceed)).ToList());

    private void SetupCli(bool success, string error = "") =>
        _dotNetRunner
            .Setup(r => r.RunAsync("tool update -g w3ti.OpenBase.CLI", It.IsAny<CancellationToken>()))
            .ReturnsAsync((success, error));

    private void SetupToolVersion(string? version) =>
        _dotNetRunner
            .Setup(r => r.GetInstalledToolVersionAsync("w3ti.openbase.cli", It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

    private void SetupTemplateVersion(string? version) =>
        _dotNetRunner
            .Setup(r => r.GetInstalledTemplateVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

    [Fact]
    public async Task ExecuteAsync_PackagesAndCliSucceed_ReturnsZero()
    {
        SetupPackages(true);
        SetupCli(true);
        SetupToolVersion("10.5.11");
        SetupTemplateVersion("2.0.0");

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesFail_CliSucceeds_ReturnsOne()
    {
        SetupPackages(false);
        SetupCli(true);
        SetupToolVersion("10.5.11");
        SetupTemplateVersion(null);

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesSucceed_CliFails_WithoutErrorMessage_ReturnsOne()
    {
        SetupPackages(true);
        SetupCli(false, string.Empty);
        SetupTemplateVersion("2.0.0");

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesSucceed_CliFails_WithErrorMessage_ReturnsOne()
    {
        SetupPackages(true);
        SetupCli(false, "Erro ao conectar ao servidor NuGet");
        SetupTemplateVersion("2.0.0");

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_BothFail_ReturnsOne()
    {
        SetupPackages(false);
        SetupCli(false, "Rede indisponível");
        SetupTemplateVersion(null);

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_CallsPackageRunnerOnce()
    {
        SetupPackages(true);
        SetupCli(true);
        SetupToolVersion(null);
        SetupTemplateVersion(null);

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        _packageRunner.Verify(r => r.RunPackagesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SavesOneHistoryEntryPerComponent()
    {
        SetupPackages(true);
        SetupCli(true);
        SetupToolVersion("10.5.11");
        SetupTemplateVersion("2.0.0");

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        var expectedCount = DotNet.TemplatePackages.Length + 1;
        _historyService.Verify(h => h.AddEntryAsync(
            It.IsAny<UpdateHistoryEntry>(), It.IsAny<CancellationToken>()),
            Times.Exactly(expectedCount));
    }

    [Fact]
    public async Task ExecuteAsync_OnCliSuccess_CliHistoryEntryHasNewVersion()
    {
        SetupPackages(true);
        SetupCli(true);
        SetupToolVersion("10.5.11");
        SetupTemplateVersion(null);

        UpdateHistoryEntry? cliEntry = null;
        _historyService
            .Setup(h => h.AddEntryAsync(It.Is<UpdateHistoryEntry>(e => e.Component == "w3ti.OpenBase.CLI"), It.IsAny<CancellationToken>()))
            .Callback<UpdateHistoryEntry, CancellationToken>((e, _) => cliEntry = e);

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.NotNull(cliEntry);
        Assert.True(cliEntry.Success);
        Assert.Equal("10.5.11", cliEntry.NewVersion);
    }

    [Fact]
    public async Task ExecuteAsync_OnCliFailure_CliHistoryEntryHasNoNewVersion()
    {
        SetupPackages(true);
        SetupCli(false);
        SetupTemplateVersion(null);

        UpdateHistoryEntry? cliEntry = null;
        _historyService
            .Setup(h => h.AddEntryAsync(It.Is<UpdateHistoryEntry>(e => e.Component == "w3ti.OpenBase.CLI"), It.IsAny<CancellationToken>()))
            .Callback<UpdateHistoryEntry, CancellationToken>((e, _) => cliEntry = e);

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.NotNull(cliEntry);
        Assert.False(cliEntry.Success);
        Assert.Null(cliEntry.NewVersion);
    }

    [Fact]
    public async Task ExecuteAsync_OnTemplateSuccess_TemplateHistoryEntryHasNewVersion()
    {
        SetupPackages(true);
        SetupCli(true);
        SetupToolVersion(null);
        SetupTemplateVersion("2.1.0");

        var templateEntries = new List<UpdateHistoryEntry>();
        _historyService
            .Setup(h => h.AddEntryAsync(It.Is<UpdateHistoryEntry>(e => e.Component != "w3ti.OpenBase.CLI"), It.IsAny<CancellationToken>()))
            .Callback<UpdateHistoryEntry, CancellationToken>((e, _) => templateEntries.Add(e));

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(DotNet.TemplatePackages.Length, templateEntries.Count);
        Assert.All(templateEntries, e =>
        {
            Assert.True(e.Success);
            Assert.Equal("2.1.0", e.NewVersion);
        });
    }
}
