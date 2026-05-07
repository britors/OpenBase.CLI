using OpenBase.CLI.Helpers;
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
    public async Task RunPackagesAsync_AllSucceed_ReturnsFalse()
    {
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        var result = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.False(result);
        _dotNetRunner.Verify(
            r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(DotNet.TemplatePackages.Length));
    }

    [Fact]
    public async Task RunPackagesAsync_FirstPackageFails_WithoutErrorMessage_ReturnsTrue()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, string.Empty))
            .ReturnsAsync((true, string.Empty));

        var result = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RunPackagesAsync_FirstPackageFails_WithErrorMessage_ReturnsTrue()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Erro de rede"))
            .ReturnsAsync((true, string.Empty));

        var result = await CreateRunner().RunPackagesAsync(
            "Instalando", "instalado", "instalar", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RunPackagesAsync_SecondPackageFails_ReturnsTrue()
    {
        _dotNetRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty))
            .ReturnsAsync((false, "Timeout"));

        var result = await CreateRunner().RunPackagesAsync(
            "Atualizando", "atualizado", "atualizar", CancellationToken.None);

        Assert.True(result);
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
}
