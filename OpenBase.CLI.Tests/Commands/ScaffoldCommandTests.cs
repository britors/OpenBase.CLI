using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class ScaffoldCommandTests
{
    private readonly Mock<IProjectLocator> _locator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    private ScaffoldCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _locator.Object, _fileWriter.Object);

    private static ScaffoldSettings BuildSettings(string entity = "Produto", string? ns = null) =>
        new() { Entity = entity, RootNamespace = ns };

    private void SetupLocator(string? solutionDir, string? rootNs) =>
        _locator
            .Setup(l => l.Detect(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((solutionDir, rootNs));

    private Task<int> Run(ScaffoldSettings settings) =>
        ((ICommand<ScaffoldSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("scaffold"), settings, CancellationToken.None);

    [Fact]
    public async Task Execute_ProjectNotFound_ReturnsOne()
    {
        SetupLocator(null, null);

        var result = await Run(BuildSettings());

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ValidProject_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(BuildSettings("Produto"));

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_ValidProject_Creates34Files()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto"));

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(34));
    }

    [Fact]
    public async Task Execute_FileAlreadyExists_SkipsFile()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var result = await Run(BuildSettings("Produto"));

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WriteThrows_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter
            .Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Disco cheio"));

        var result = await Run(BuildSettings("Produto"));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_PassesNamespaceOverrideToLocator()
    {
        SetupLocator("/solution", "MeuNS");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto", "MeuNS"));

        _locator.Verify(l => l.Detect(It.IsAny<string>(), "MeuNS"), Times.Once);
    }

    [Fact]
    public async Task Execute_EnsuresDirectoriesBeforeWriting()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto"));

        _fileWriter.Verify(f => f.EnsureDirectory(It.IsAny<string>()), Times.AtLeastOnce);
    }
}
