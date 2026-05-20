using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class ProcedureCommandTests
{
    private readonly Mock<IProjectLocator>       _locator            = new();
    private readonly Mock<IFileWriter>           _fileWriter         = new();
    private readonly Mock<IDbFlavorDetector>     _dbFlavorDetector   = new();
    private readonly Mock<IConnectionStringReader> _connStringReader  = new();
    private readonly Mock<IDbSchemaReader>       _dbSchemaReader     = new();

    public ProcedureCommandTests()
    {
        _dbFlavorDetector
            .Setup(d => d.Detect(It.IsAny<string>()))
            .Returns(DbFlavor.SqlServer);

        _connStringReader
            .Setup(r => r.Read(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null);

        _dbSchemaReader
            .Setup(r => r.ReadProcedureParameters(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>()))
            .Returns([]);
    }

    private ProcedureCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _locator.Object, _fileWriter.Object,
            _dbFlavorDetector.Object, _connStringReader.Object, _dbSchemaReader.Object);

    private static ProcedureSettings BuildSettings(string? name = "GetOrderById", string? ns = null) =>
        new() { Name = name, RootNamespace = ns };

    private void SetupLocator(string? solutionDir, string? rootNs) =>
        _locator
            .Setup(l => l.Detect(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((solutionDir, rootNs));

    private Task<int> Run(ProcedureSettings settings) =>
        ((ICommand<ProcedureSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("procedure"), settings, CancellationToken.None);

    [Fact]
    public async Task Execute_ProjectNotFound_ReturnsOne()
    {
        SetupLocator(null, null);

        var result = await Run(BuildSettings());

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_NoName_NonInteractive_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(BuildSettings(name: null));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_InvalidPascalCase_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(BuildSettings(name: "getOrderById"));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_ValidName_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(BuildSettings("GetOrderById"));

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_ValidName_Creates8Files()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("GetOrderById"));

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(8));
    }

    [Fact]
    public async Task Execute_FileAlreadyExists_SkipsFile()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var result = await Run(BuildSettings("GetOrderById"));

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

        var result = await Run(BuildSettings("GetOrderById"));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_EnsuresDirectoriesBeforeWriting()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("GetOrderById"));

        _fileWriter.Verify(f => f.EnsureDirectory(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_WithConnectionString_ReadsParametersFromDb()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _connStringReader
            .Setup(r => r.Read(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("Server=.;Database=Test;");

        await Run(BuildSettings("GetOrderById"));

        _dbSchemaReader.Verify(
            r => r.ReadProcedureParameters("Server=.;Database=Test;", It.IsAny<string>(), "GetOrderById", DbFlavor.SqlServer),
            Times.Once);
    }

    [Fact]
    public async Task Execute_NoConnectionString_SkipsDbRead()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("GetOrderById"));

        _dbSchemaReader.Verify(
            r => r.ReadProcedureParameters(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_DetectsDbFlavorFromSolutionDir()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("GetOrderById"));

        _dbFlavorDetector.Verify(d => d.Detect("/solution"), Times.Once);
    }

    [Fact]
    public async Task Execute_PassesNamespaceOverrideToLocator()
    {
        SetupLocator("/solution", "MeuNS");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("GetOrderById", "MeuNS"));

        _locator.Verify(l => l.Detect(It.IsAny<string>(), "MeuNS"), Times.Once);
    }
}
