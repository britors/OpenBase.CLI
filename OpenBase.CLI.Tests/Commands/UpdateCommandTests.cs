using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class UpdateCommandTests
{
    private readonly Mock<ITemplatePackageRunner> _packageRunner = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    private UpdateCommand CreateCommand() =>
        new(_packageRunner.Object, _dotNetRunner.Object, CommandTestHelper.CreateConsole());

    private void SetupPackages(bool failed) =>
        _packageRunner
            .Setup(r => r.RunPackagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failed);

    private void SetupCli(bool success, string error = "") =>
        _dotNetRunner
            .Setup(r => r.RunAsync("tool update -g w3ti.OpenBase.CLI", It.IsAny<CancellationToken>()))
            .ReturnsAsync((success, error));

    [Fact]
    public async Task ExecuteAsync_PackagesAndCliSucceed_ReturnsZero()
    {
        SetupPackages(false);
        SetupCli(true);

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesFail_CliSucceeds_ReturnsOne()
    {
        SetupPackages(true);
        SetupCli(true);

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesSucceed_CliFails_WithoutErrorMessage_ReturnsOne()
    {
        SetupPackages(false);
        SetupCli(false, string.Empty);

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PackagesSucceed_CliFails_WithErrorMessage_ReturnsOne()
    {
        SetupPackages(false);
        SetupCli(false, "Erro ao conectar ao servidor NuGet");

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_BothFail_ReturnsOne()
    {
        SetupPackages(true);
        SetupCli(false, "Rede indisponível");

        var result = await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectVerbsToPackageRunner()
    {
        SetupPackages(false);
        SetupCli(true);

        await ((ICommand<UpdateSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("update"), new UpdateSettings(), CancellationToken.None);

        _packageRunner.Verify(r => r.RunPackagesAsync(
            "Atualizando", "atualizado", "atualizar", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
