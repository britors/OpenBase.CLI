using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class VersionCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    private VersionCommand CreateCommand() =>
        new(_dotNetRunner.Object, CommandTestHelper.CreateConsole());

    private void SetupDefaults(
        string dotnetVersion = "10.0.0",
        string? cliVersion = null,
        string? sqlVersion = null,
        string? pgVersion = null)
    {
        _dotNetRunner.Setup(r => r.GetDotnetVersion()).Returns(dotnetVersion);
        _dotNetRunner.Setup(r => r.GetInstalledToolVersionAsync("w3ti.OpenBase.CLI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliVersion);
        _dotNetRunner.Setup(r => r.GetInstalledTemplateVersionAsync("w3ti.OpenBaseNET.SQLServer.Template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sqlVersion);
        _dotNetRunner.Setup(r => r.GetInstalledTemplateVersionAsync("w3ti.OpenBaseNET.Postgres.Template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pgVersion);
    }

    [Fact]
    public async Task Execute_ReturnsZero()
    {
        SetupDefaults();

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_CallsGetDotnetVersion()
    {
        SetupDefaults(dotnetVersion: "10.0.1");

        await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        _dotNetRunner.Verify(r => r.GetDotnetVersion(), Times.Once);
    }

    [Fact]
    public async Task Execute_QueriesInstalledVersionsForAllTrackedComponents()
    {
        SetupDefaults();

        await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        _dotNetRunner.Verify(r => r.GetInstalledToolVersionAsync("w3ti.OpenBase.CLI", It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.GetInstalledTemplateVersionAsync("w3ti.OpenBaseNET.SQLServer.Template", It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.GetInstalledTemplateVersionAsync("w3ti.OpenBaseNET.Postgres.Template", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithInstalledVersions_ReturnsZero()
    {
        SetupDefaults(cliVersion: "10.6.2", sqlVersion: "10.3.1", pgVersion: "10.3.0");

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_WithNoInstalledVersions_ReturnsZero()
    {
        SetupDefaults();

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("show"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }
}
