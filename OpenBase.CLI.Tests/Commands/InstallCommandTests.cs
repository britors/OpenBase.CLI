using Moq;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class InstallCommandTests
{
    private readonly Mock<ITemplatePackageRunner> _runner = new();

    private InstallCommand CreateCommand() => new(_runner.Object);

    private void SetupPackages(bool allSucceed) =>
        _runner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DotNet.TemplatePackages.Select(p => (p, allSucceed)).ToList());

    [Fact]
    public async Task ExecuteAsync_AllPackagesSucceed_ReturnsZero()
    {
        SetupPackages(true);

        var result = await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackageFails_ReturnsOne()
    {
        SetupPackages(false);

        var result = await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_CallsRunnerOnce()
    {
        SetupPackages(true);

        await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        _runner.Verify(r => r.RunPackagesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
