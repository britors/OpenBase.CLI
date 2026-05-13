using OpenBase.CLI.Helpers.Execution;
using Spectre.Console;

namespace OpenBase.CLI.Tests.Helpers;

public class TemplatePackageRunnerTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    private TemplatePackageRunner CreateRunner() =>
        new(_dotNetRunner.Object, AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No
        }));

    [Fact]
    public async Task RunPackagesAsync_AllSucceed_ReturnsAllSuccess()
    {
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        var results = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.Equal(DotNet.TemplatePackages.Length, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task RunPackagesAsync_FirstPackageFails_WithoutErrorMessage_ReturnsFirstFailed()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, string.Empty))
            .ReturnsAsync((true, string.Empty));

        var results = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.False(results[0].Success);
        Assert.True(results[1].Success);
    }

    [Fact]
    public async Task RunPackagesAsync_FirstPackageFails_WithErrorMessage_ReturnsFirstFailed()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Erro de rede"))
            .ReturnsAsync((true, string.Empty));

        var results = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.False(results[0].Success);
    }

    [Fact]
    public async Task RunPackagesAsync_SecondPackageFails_ReturnsSecondFailed()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty))
            .ReturnsAsync((false, "Timeout"));

        var results = await CreateRunner().RunPackagesAsync(
            "Atualizando", "atualizado", "atualizar", CancellationToken.None);

        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
    }

    [Fact]
    public async Task RunPackagesAsync_PassesCorrectArgumentsForEachPackage()
    {
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        foreach (var package in DotNet.TemplatePackages)
            _dotNetRunner.Verify(
                r => r.RunAsync($"new install {package}", It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task RunPackagesAsync_ReturnsOneResultPerPackage()
    {
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        var results = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.Equal(DotNet.TemplatePackages.Length, results.Count);
        foreach (var package in DotNet.TemplatePackages)
            Assert.Contains(results, r => r.PackageId == package);
    }
}
