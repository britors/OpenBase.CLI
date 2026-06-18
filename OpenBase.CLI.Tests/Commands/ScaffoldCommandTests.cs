using OpenBase.CLI.Commands;
using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.Interactive;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class ScaffoldCommandTests
{
    private readonly Mock<IProjectLocator> _locator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();
    private readonly Mock<IEntityPropertyCollector> _propertyCollector = new();
    private readonly Mock<IDbFlavorDetector> _dbFlavorDetector = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IModelFirstPropertyCollector> _modelFirstCollector = new();

    public ScaffoldCommandTests()
    {
        _propertyCollector
            .Setup(c => c.Collect(It.IsAny<DbFlavor>()))
            .Returns([new EntityProperty("Name", "string", true)]);

        _dbFlavorDetector
            .Setup(d => d.Detect(It.IsAny<string>()))
            .Returns(DbFlavor.SqlServer);

        _dotNetRunner
            .Setup(r => r.Run(It.IsAny<string>()))
            .Returns((true, string.Empty));
    }

    private ScaffoldCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _locator.Object, _fileWriter.Object,
            _propertyCollector.Object, _dbFlavorDetector.Object, _dotNetRunner.Object,
            _modelFirstCollector.Object);

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
    public async Task Execute_ValidProject_Creates47Files()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto"));

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(47));
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

    [Fact]
    public async Task Execute_DetectsDbFlavorFromSolutionDir()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto"));

        _dbFlavorDetector.Verify(d => d.Detect("/solution"), Times.Once);
    }

    [Fact]
    public async Task Execute_PassesDetectedFlavorToPropertyCollector()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _dbFlavorDetector.Setup(d => d.Detect(It.IsAny<string>())).Returns(DbFlavor.Postgres);

        await Run(BuildSettings("Produto"));

        _propertyCollector.Verify(c => c.Collect(DbFlavor.Postgres), Times.Once);
    }


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
        return (new ScaffoldCommand(console, _locator.Object, _fileWriter.Object,
            _propertyCollector.Object, _dbFlavorDetector.Object, _dotNetRunner.Object,
            _modelFirstCollector.Object), writer);
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

        Assert.Contains(SR.Current.FilesCreated.Replace("{0} ", string.Empty), output.ToString());
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

        Assert.Contains(SR.Current.FilesSkipped.Replace("{0} ", string.Empty), output.ToString());
    }

    [Fact]
    public async Task PrintFileList_SkippedFiles_DoesNotPrintCreatedHeader()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.DoesNotContain(SR.Current.FilesCreated.Replace("{0} ", string.Empty), output.ToString());
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

        Assert.Contains(SR.Current.FilesErrors.Replace("{0} ", string.Empty), output.ToString());
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


    [Fact]
    public async Task Execute_ValidProject_AddsCsprojToSolution()
    {
        SetupLocator("/solution", "OpenBaseNET");
        var csprojPath = Path.Combine("/solution", "tests", "OpenBaseNET.Tests.Unit", "OpenBaseNET.Tests.Unit.csproj");
        var slnPath = Path.Combine("/solution", "OpenBaseNET.sln");

        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.FindSolutionFile("/solution")).Returns(slnPath);
        _fileWriter.Setup(f => f.FileExists(csprojPath)).Returns(true);

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(
            r => r.Run(It.Is<string>(a => a.Contains("sln") && a.Contains(slnPath) && a.Contains(csprojPath))),
            Times.Once);
    }

    [Fact]
    public async Task Execute_NoSolutionFile_SkipsSlnAdd()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.FindSolutionFile(It.IsAny<string>())).Returns((string?)null);

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_CsprojNotFound_SkipsSlnAdd()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.FindSolutionFile("/solution")).Returns("/solution/OpenBaseNET.sln");

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.IsAny<string>()), Times.Never);
    }


    private static ScaffoldSettings BuildModelFirstSettings(
        string entity = "Produto", string schema = "dbo", string table = "produtos") =>
        new() { Entity = entity, Mode = "modelfirst", Schema = schema, Table = table };

    [Fact]
    public async Task Execute_ModelFirstMode_UsesModelFirstCollector()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _modelFirstCollector
            .Setup(c => c.Collect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(([new EntityProperty("Name", "string", true)], "produtos", "dbo"));

        var result = await Run(BuildModelFirstSettings());

        Assert.Equal(0, result);
        _modelFirstCollector.Verify(c => c.Collect(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _propertyCollector.Verify(c => c.Collect(It.IsAny<DbFlavor>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ModelFirstMode_PassesSchemaAndTableToCollector()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _modelFirstCollector
            .Setup(c => c.Collect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(), "dbo", "produtos"))
            .Returns(([new EntityProperty("Name", "string", true)], "produtos", "dbo"));

        var result = await Run(BuildModelFirstSettings(schema: "dbo", table: "produtos"));

        Assert.Equal(0, result);
        _modelFirstCollector.Verify(c => c.Collect(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(), "dbo", "produtos"), Times.Once);
    }

    [Fact]
    public async Task Execute_ModelFirstMode_ReturnsOne_WhenCollectorReturnsNull()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _modelFirstCollector
            .Setup(c => c.Collect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(((IReadOnlyList<EntityProperty>, string, string)?)null);

        var result = await Run(BuildModelFirstSettings());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_InvalidMode_ReturnsOne()
    {
        SetupLocator("/solution", "OpenBaseNET");

        var result = await Run(new ScaffoldSettings { Entity = "Produto", Mode = "invalid" });

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_CodefirstModeFlag_UsesPropertyCollector()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await Run(new ScaffoldSettings { Entity = "Produto", Mode = "codefirst" });

        Assert.Equal(0, result);
        _propertyCollector.Verify(c => c.Collect(It.IsAny<DbFlavor>()), Times.Once);
        _modelFirstCollector.Verify(c => c.Collect(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DbFlavor>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    private static string DbContextPath(string ns = "OpenBaseNET") =>
        Path.Combine("/solution", "src", $"{ns}.Infra.Data.Context", "OneBaseDataBaseContext.cs");

    private const string DbContextTemplate = """
        using Microsoft.EntityFrameworkCore;

        namespace OpenBaseNET.Infra.Data.Context;

        public class OneBaseDataBaseContext(IConfiguration configuration) : DbContext
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder) { }
        }
        """;

    private void SetupDbContext(string content)
    {
        var path = DbContextPath();
        _fileWriter.Setup(f => f.FileExists(path)).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(path)).Returns(content);
    }

    private DbSetInjectionResult RunInjectDbSet(string entity = "Produto", string ns = "OpenBaseNET")
    {
        var ctx = new ScaffoldContext(entity, ns, "/solution");
        return CreateCommand().InjectDbSet(ctx);
    }

    [Fact]
    public void InjectDbSet_ReturnsFileNotFound_WhenContextMissing()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = RunInjectDbSet();

        Assert.Equal(DbSetInjectionResult.FileNotFound, result);
    }

    [Fact]
    public void InjectDbSet_ReturnsAlreadyExists_WhenDbSetPresent()
    {
        SetupDbContext("""public DbSet<Produto> Produtos { get; set; }""");

        var result = RunInjectDbSet();

        Assert.Equal(DbSetInjectionResult.AlreadyExists, result);
        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void InjectDbSet_ReturnsInjected_AndWritesDbSet()
    {
        SetupDbContext(DbContextTemplate);
        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(DbContextPath(), It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        var result = RunInjectDbSet();

        Assert.Equal(DbSetInjectionResult.Injected, result);
        Assert.Contains("DbSet<Produto>", written);
        Assert.Contains("Produtos", written);
    }

    [Fact]
    public void InjectDbSet_AddsEntityUsing_WhenNotPresent()
    {
        SetupDbContext(DbContextTemplate);
        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(DbContextPath(), It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        RunInjectDbSet();

        Assert.Contains("using OpenBaseNET.Domain.Entities;", written);
    }

    [Fact]
    public void InjectDbSet_DoesNotDuplicateUsing_WhenAlreadyPresent()
    {
        SetupDbContext(DbContextTemplate.Replace(
            "using Microsoft.EntityFrameworkCore;",
            "using Microsoft.EntityFrameworkCore;\nusing OpenBaseNET.Domain.Entities;"));
        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(DbContextPath(), It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        RunInjectDbSet();

        var count = written!.Split("using OpenBaseNET.Domain.Entities;").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public void InjectDbSet_InsertsAfterLastDbSet_WhenMultipleExist()
    {
        var content = DbContextTemplate.Replace(
            "    protected override void OnModelCreating",
            "    public DbSet<Categoria> Categorias { get; set; }\n    protected override void OnModelCreating");
        SetupDbContext(content);
        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(DbContextPath(), It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        RunInjectDbSet();

        var categoriaIdx = written!.IndexOf("DbSet<Categoria>", StringComparison.Ordinal);
        var produtoIdx   = written.IndexOf("DbSet<Produto>",   StringComparison.Ordinal);
        Assert.True(categoriaIdx < produtoIdx);
    }

    [Fact]
    public void InjectDbSet_ReturnsFailed_WhenContentIsEmpty()
    {
        _fileWriter.Setup(f => f.FileExists(DbContextPath())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(DbContextPath())).Returns(string.Empty);

        var result = RunInjectDbSet();

        Assert.Equal(DbSetInjectionResult.Failed, result);
    }


    private void SetupForMigration()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        SetupDbContext(DbContextTemplate);
    }

    [Fact]
    public async Task Execute_RunsMigrationAdd_WhenDbSetInjected()
    {
        SetupForMigration();

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.Is<string>(a =>
            a.Contains("ef migrations add") && a.Contains("AddProduto"))), Times.Once);
    }

    [Fact]
    public async Task Execute_MigrationAdd_IncludesProjectAndStartupProjectFlags()
    {
        SetupForMigration();

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.Is<string>(a =>
            a.Contains("--project") && a.Contains("Infra.Data.Context") &&
            a.Contains("--startup-project") && a.Contains("Presentation.Api"))), Times.Once);
    }

    [Fact]
    public async Task Execute_DoesNotRunMigration_WhenDbSetNotInjected()
    {
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.Is<string>(a => a.Contains("ef"))), Times.Never);
    }

    [Fact]
    public async Task Execute_ShowsMigrationError_WhenMigrationFails()
    {
        var (cmd, output) = CreateCommandWithOutput();
        SetupLocator("/solution", "OpenBaseNET");
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        SetupDbContext(DbContextTemplate);
        _dotNetRunner
            .Setup(r => r.Run(It.Is<string>(a => a.Contains("migrations add"))))
            .Returns((false, "Build failed."));

        await RunWithOutput(cmd, BuildSettings("Produto"));

        Assert.Contains("Erro", output.ToString());
    }

    [Fact]
    public async Task Execute_DoesNotAskForDatabaseUpdate_WhenNonInteractive()
    {
        SetupForMigration();

        await Run(BuildSettings("Produto"));

        _dotNetRunner.Verify(r => r.Run(It.Is<string>(a => a.Contains("database update"))), Times.Never);
    }


    [Fact]
    public void EmptyMigrationUpMethod_RemovesUpBody()
    {
        var content = """
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable(
                    name: "Customer",
                    columns: table => new { Id = table.Column<int>() },
                    constraints: table => { table.PrimaryKey("PK_Customer", x => x.Id); });
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(name: "Customer");
            }
            """;

        var result = DbContextEditor.EmptyMigrationUpMethod(content);

        Assert.Contains("protected override void Up(MigrationBuilder migrationBuilder)", result);
        Assert.DoesNotContain("CreateTable", result);
        Assert.Contains("DropTable", result);
    }

    [Fact]
    public void EmptyMigrationUpMethod_ReturnsOriginal_WhenUpNotFound()
    {
        var content = "public class Foo { }";
        var result = DbContextEditor.EmptyMigrationUpMethod(content);
        Assert.Equal(content, result);
    }

    [Fact]
    public void EmptyMigrationUpMethod_HandlesNestedBraces()
    {
        var content = """
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable("T", t => new
                {
                    Id = t.Column<int>()
                }, c => { c.PrimaryKey("PK", x => x.Id); });
            }
            """;

        var result = DbContextEditor.EmptyMigrationUpMethod(content);

        Assert.DoesNotContain("CreateTable", result);
    }

}
