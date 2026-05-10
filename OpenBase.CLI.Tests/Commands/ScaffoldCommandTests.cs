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

    // ── PrintFileList ─────────────────────────────────────────────────────────

    private (ScaffoldCommand Command, StringWriter Output) CreateCommandWithOutput()
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        return (new ScaffoldCommand(console, _locator.Object, _fileWriter.Object), writer);
    }

    private Task<int> RunWithOutput(ScaffoldCommand command, ScaffoldSettings settings) =>
        ((ICommand<ScaffoldSettings>)command)
            .ExecuteAsync(CommandTestHelper.CreateContext("scaffold"), settings, CancellationToken.None);

    [Fact]
    public async Task PrintFileList_CreatedFiles_PrintsCountAndHeader()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("arquivo(s) criado(s):", output.ToString());
    }

    [Fact]
    public async Task PrintFileList_CreatedFiles_PrintsRelativePath()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("Produto", output.ToString());
    }

    [Fact]
    public async Task PrintFileList_SkippedFiles_PrintsSkippedHeader()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("já existente(s) ignorado(s):", output.ToString());
    }

    [Fact]
    public async Task PrintFileList_SkippedFiles_DoesNotPrintCreatedHeader()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.DoesNotContain("arquivo(s) criado(s):", output.ToString());
    }

    [Fact]
    public async Task PrintFileList_FailedFiles_PrintsErrorHeader()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter
            .Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Disco cheio"));

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("erro(s):", output.ToString());
    }

    [Fact]
    public async Task PrintFileList_FailedFiles_PrintsExceptionMessage()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter
            .Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("Disco cheio"));

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("Disco cheio", output.ToString());
    }
}
