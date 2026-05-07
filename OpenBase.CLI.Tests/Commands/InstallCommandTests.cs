using Moq;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class InstallCommandTests
{
    private readonly Mock<ITemplatePackageRunner> _runner = new();

    private InstallCommand CreateCommand() => new(_runner.Object);

    [Fact]
    public async Task ExecuteAsync_AllPackagesSucceed_ReturnsZero()
    {
        _runner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackageFails_ReturnsOne()
    {
        _runner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectVerbsToRunner()
    {
        _runner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await ((ICommand<InstallSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("install"), new InstallSettings(), CancellationToken.None);

        _runner.Verify(r => r.RunPackagesAsync(
            "Instalando", "instalado", "instalar", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
