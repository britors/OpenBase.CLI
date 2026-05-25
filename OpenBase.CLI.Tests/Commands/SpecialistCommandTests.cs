using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.IO;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class SpecialistCommandTests
{
    private readonly Mock<IProjectLocator> _locator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    private SpecialistCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _locator.Object, _fileWriter.Object);

    private void SetupLocator(string? solutionDir, string? rootNs) =>
        _locator
            .Setup(l => l.Detect(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((solutionDir, rootNs));

    private Task<int> Run(SpecialistSettings settings) =>
        ((ICommand<SpecialistSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("specialist"), settings, CancellationToken.None);

    [Fact]
    public async Task Execute_ProjectNotFound_ReturnsOne()
    {
        SetupLocator(null, null);

        var result = await Run(new SpecialistSettings { Entity = "Produto", Method = "GetByCategoria" });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_NonInteractive_NoMethod_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings { Entity = "Produto" });

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_HttpCall_NoSqlRequired_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "SyncExternal",
            Type   = "httpcall"
        });

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_Query_DefaultType_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetByCategoria",
            Sql     = "SELECT Name FROM Products WHERE CategoriaId = {{categoriaId}}",
            Params  = ["categoriaId:Guid"],
            Columns = ["Nome:string", "Preco:decimal"]
        });

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_Query_Paginated_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity    = "Produto",
            Method    = "GetPagedByCategoria",
            Type      = "query",
            Sql       = "SELECT Name FROM Products WHERE CategoriaId = {{categoriaId}}",
            Params    = ["categoriaId:Guid"],
            Columns   = ["Nome:string"],
            Paginated = true
        });

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_Command_NoColumns_WritesFiles()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "Deactivate",
            Type   = "command",
            Sql    = "UPDATE Products SET Active = 0 WHERE Id = {{id}}",
            Params = ["id:Guid"]
        });

        Assert.Equal(0, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_QueryWithoutSql_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "GetByCategoria",
            Type   = "query"
        });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("GetById")]
    [InlineData("Add")]
    [InlineData("RemoveById")]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("FindByArgumentsPaged")]
    public async Task Execute_ReservedMethodName_ReturnsOne(string reservedMethod)
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = reservedMethod,
            Type   = "query",
            Sql    = "SELECT 1"
        });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_InvalidType_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "GetByCategoria",
            Type   = "invalid"
        });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_InvalidParamFormat_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "GetByCategoria",
            Type   = "query",
            Sql    = "SELECT Name FROM Products WHERE Id = {{id}}",
            Params = ["malformado"]
        });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_InvalidColumnFormat_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetByCategoria",
            Type    = "query",
            Sql     = "SELECT Name FROM Products",
            Columns = ["semDoisPontos"]
        });

        Assert.Equal(1, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ParamNotInSql_IsIgnored()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetAll",
            Type    = "query",
            Sql     = "SELECT Name FROM Products",
            Params  = ["extraParam:string"],
            Columns = ["Nome:string"]
        });

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_ParamMissingType_DefaultsToString()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetByCategoria",
            Type    = "query",
            Sql     = "SELECT Name FROM Products WHERE CategoriaId = {{categoriaId}}",
            Columns = ["Nome:string"]
        });

        Assert.Equal(0, result);
    }

    // ─── Multi-line SQL ────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_Query_MultiLineSql_GeneratesRawStringLiteral()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        string? repositoryContent = null;
        _fileWriter
            .Setup(f => f.WriteAllText(
                It.Is<string>(p => p.EndsWith("ProdutoRepository.GetByCategoria.cs")),
                It.IsAny<string>()))
            .Callback<string, string>((_, c) => repositoryContent = c);

        await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetByCategoria",
            Type    = "query",
            Sql     = "SELECT Name\nFROM Products\nWHERE CategoriaId = {{categoriaId}}",
            Params  = ["categoriaId:Guid"],
            Columns = ["Nome:string"]
        });

        Assert.NotNull(repositoryContent);
        Assert.Contains("\"\"\"", repositoryContent);
        Assert.DoesNotContain("\"SELECT Name\\nFROM", repositoryContent);
    }

    [Fact]
    public async Task Execute_Command_MultiLineSql_GeneratesRawStringLiteral()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        string? repositoryContent = null;
        _fileWriter
            .Setup(f => f.WriteAllText(
                It.Is<string>(p => p.EndsWith("ProdutoRepository.Deactivate.cs")),
                It.IsAny<string>()))
            .Callback<string, string>((_, c) => repositoryContent = c);

        await Run(new SpecialistSettings
        {
            Entity = "Produto",
            Method = "Deactivate",
            Type   = "command",
            Sql    = "UPDATE Products\nSET Active = 0\nWHERE Id = {{id}}",
            Params = ["id:Guid"]
        });

        Assert.NotNull(repositoryContent);
        Assert.Contains("\"\"\"", repositoryContent);
    }

    [Fact]
    public async Task Execute_Query_SingleLineSql_GeneratesRegularStringLiteral()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        string? repositoryContent = null;
        _fileWriter
            .Setup(f => f.WriteAllText(
                It.Is<string>(p => p.EndsWith("ProdutoRepository.GetByCategoria.cs")),
                It.IsAny<string>()))
            .Callback<string, string>((_, c) => repositoryContent = c);

        await Run(new SpecialistSettings
        {
            Entity  = "Produto",
            Method  = "GetByCategoria",
            Type    = "query",
            Sql     = "SELECT Name FROM Products WHERE CategoriaId = {{categoriaId}}",
            Params  = ["categoriaId:Guid"],
            Columns = ["Nome:string"]
        });

        Assert.NotNull(repositoryContent);
        Assert.Contains("\"SELECT Name FROM Products", repositoryContent);
        Assert.DoesNotContain("\"\"\"", repositoryContent);
    }
}
