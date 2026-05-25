using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.MongoDB;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;

namespace OpenBase.CLI.Tests.Commands;

public class MongoDbExtensionHandlerTests
{
    private readonly Mock<IAnsiConsole> _console = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    public MongoDbExtensionHandlerTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.Run(It.IsAny<string>())).Returns((true, string.Empty));
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
    }

    private MongoDbExtensionHandler CreateHandler() =>
        new(_console.Object, _dotNetRunner.Object, _fileWriter.Object);

    private static ExtensionContext BuildContext(string? solutionDir = "/solution", string? ns = "MyApp") =>
        new(null, solutionDir ?? "/solution", null, [], solutionDir, ns);

    // --- Basic ---

    [Fact]
    public void Apply_SolutionDirIsNull_ReturnsError()
    {
        var result = CreateHandler().Apply(BuildContext(null));

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Apply_RootNamespaceIsNull_ReturnsError()
    {
        var result = CreateHandler().Apply(BuildContext("solution", null));

        Assert.False(result.Success);
    }

    [Fact]
    public void Apply_ValidContext_ReturnsSuccess()
    {
        var result = CreateHandler().Apply(BuildContext());

        Assert.True(result.Success);
    }

    [Fact]
    public void Name_IsMongodb()
    {
        Assert.Equal("mongodb", CreateHandler().Name);
    }

    [Fact]
    public void SupportedProviders_IsEmpty()
    {
        Assert.Empty(CreateHandler().SupportedProviders);
    }

    // --- File generation ---

    [Fact]
    public void Apply_CreatesIMongoDbContextFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("IMongoDbContext.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesMongoDbContextFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("MongoDbContext.cs") && !p.EndsWith("IMongoDbContext.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesMongoDbExtensionsFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("MongoDbExtensions.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_FileExists_SkipsCreation()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddMongoDb");

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("IMongoDbContext.cs")),
            It.IsAny<string>()), Times.Never);
    }

    // --- Infra.MongoDb project creation ---

    [Fact]
    public void Apply_CreatesInfraMongoDbProject_WhenNotExists()
    {
        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("new classlib") && s.Contains("Infra.MongoDb"))),
            Times.Once);
    }

    [Fact]
    public void Apply_SkipsProjectCreation_WhenAlreadyExists()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns(true);

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("new classlib"))),
            Times.Never);
    }

    [Fact]
    public void Apply_PackageInstallFails_ReturnsError()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns("<Project />");
        _dotNetRunner.Setup(r => r.Run(It.Is<string>(s => s.Contains("package")))).Returns((false, "network error"));

        var result = CreateHandler().Apply(BuildContext());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Apply_PackageInstallFails_DoesNotGenerateFiles()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns("<Project />");
        _dotNetRunner.Setup(r => r.Run(It.Is<string>(s => s.Contains("package")))).Returns((false, "network error"));

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("MongoDbContext.cs") && !p.EndsWith("IMongoDbContext.cs")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_AddsInfraMongoDbToSolution_WhenSlnFound()
    {
        _fileWriter.Setup(f => f.FindSolutionFile(It.IsAny<string>())).Returns("/solution/MyApp.sln");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("sln") && s.Contains("add") && s.Contains("Infra.MongoDb.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_SkipsSlnAdd_WhenSlnNotFound()
    {
        _fileWriter.Setup(f => f.FindSolutionFile(It.IsAny<string>())).Returns((string?)null);

        var ex = Record.Exception(() => CreateHandler().Apply(BuildContext()));

        Assert.Null(ex);
        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("sln") && s.Contains("add"))),
            Times.Never);
    }

    [Fact]
    public void Apply_AddsInfraMongoDbReferenceToApplication()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") &&
                               s.Contains("Infra.MongoDb.csproj") &&
                               s.Contains("Application.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_AddsPresentationReferenceToInfraMongoDb()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") &&
                               s.Contains("Presentation.Api.csproj") &&
                               s.Contains("Infra.MongoDb.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InstallsMongoDriverInInfraMongoDb()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.MongoDb.csproj") && s.Contains("MongoDB.Driver"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InstallsResiliencePackageInInfraMongoDb()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.MongoDb.csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.MongoDb.csproj") && s.Contains("Microsoft.Extensions.Resilience"))),
            Times.Once);
    }

    // --- Program.cs ---

    [Fact]
    public void Apply_InjectsProgramCs_WhenFileExists()
    {
        const string programCs = """
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var app = builder.Build();
            app.MapControllers();
            await app.RunAsync();
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(programCs);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("Program.cs")),
            It.Is<string>(c =>
                c.Contains("AddMongoDb") &&
                c.Contains("using MyApp.Presentation.Api.Extensions;"))),
            Times.Once);
    }

    [Fact]
    public void Apply_ProgramCs_InjectsUsingDirectiveAtTopOfFile()
    {
        const string programCs = """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.MapControllers();
            await app.RunAsync();
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(programCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        Assert.StartsWith("using MyApp.Presentation.Api.Extensions;", written);
    }

    [Fact]
    public void Apply_ProgramCs_AddMongoDbBeforeBuild()
    {
        const string programCs = """
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var app = builder.Build();
            app.MapControllers();
            await app.RunAsync();
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(programCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        var addIdx   = written!.IndexOf("AddMongoDb", StringComparison.Ordinal);
        var buildIdx = written.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(addIdx < buildIdx, "AddMongoDb should appear before builder.Build()");
    }

    [Fact]
    public void Apply_ProgramCsAlreadyConfigured_SkipsWrite()
    {
        const string alreadyDone = """
            using MyApp.Presentation.Api.Extensions;
            builder.Services.AddMongoDb(builder.Configuration);
            var app = builder.Build();
            app.MapControllers();
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(alreadyDone);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("Program.cs")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_ProgramCsNotFound_DoesNotThrow()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(false);

        var ex = Record.Exception(() => CreateHandler().Apply(BuildContext()));

        Assert.Null(ex);
    }

    // --- appsettings.json ---

    [Fact]
    public void Apply_InjectsMongoDb_IntoAppSettingsJson()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.Is<string>(c =>
                c.Contains("MongoDb") &&
                c.Contains("ConnectionString") &&
                c.Contains("DatabaseName"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InjectsRetryAndCircuitBreaker_IntoAppSettings()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.Is<string>(c =>
                c.Contains("Retry") &&
                c.Contains("MaxAttempts") &&
                c.Contains("CircuitBreaker") &&
                c.Contains("BreakDurationSeconds"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InjectsMongoDb_IntoDevelopmentJson()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.Development.json")),
            It.Is<string>(c => c.Contains("MongoDb") && c.Contains("ConnectionString"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InjectsBothAppSettingsFiles_WhenBothExist()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(appSettings);
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.Is<string>(c => c.Contains("MongoDb"))), Times.Once);
        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.Development.json")),
            It.Is<string>(c => c.Contains("MongoDb"))), Times.Once);
    }

    [Fact]
    public void Apply_AppSettingsAlreadyHasMongoDb_SkipsWrite()
    {
        const string appSettings = """{"MongoDb":{"ConnectionString":"mongodb://localhost:27017","DatabaseName":"openbase"}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(appSettings);
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith(".json")),
            It.IsAny<string>()), Times.Never);
    }

    // --- GetFiles ---

    [Fact]
    public void GetFiles_ReturnsThreeFiles()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFiles_IMongoDbContextInApplicationLayer()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Application") && f.Path.EndsWith("IMongoDbContext.cs"));
    }

    [Fact]
    public void GetFiles_MongoDbContextInInfraMongoDbLayer()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Infra.MongoDb") &&
            f.Path.EndsWith("MongoDbContext.cs") &&
            !f.Path.EndsWith("IMongoDbContext.cs"));
    }

    [Fact]
    public void GetFiles_MongoDbExtensionsInPresentationLayer()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Presentation.Api") && f.Path.EndsWith("MongoDbExtensions.cs"));
    }

    [Fact]
    public void GetFiles_IMongoDbContextContainsNamespace()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "AcmeCorp",
            "/solution/src/AcmeCorp.Application",
            "/solution/src/AcmeCorp.Infra.MongoDb",
            "/solution/src/AcmeCorp.Presentation.Api").ToList();

        var iface = files.First(f => f.Path.EndsWith("IMongoDbContext.cs"));
        Assert.Contains("AcmeCorp.Application.Interfaces.Context", iface.Content);
    }

    [Fact]
    public void GetFiles_IMongoDbContextContainsGetCollection()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var iface = files.First(f => f.Path.EndsWith("IMongoDbContext.cs"));
        Assert.Contains("GetCollection", iface.Content);
        Assert.Contains("ExecuteAsync", iface.Content);
    }

    [Fact]
    public void GetFiles_MongoDbContextUsesInfraMongoDbNamespace()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var impl = files.First(f => f.Path.EndsWith("MongoDbContext.cs") && !f.Path.EndsWith("IMongoDbContext.cs"));
        Assert.Contains("MyApp.Infra.MongoDb.Context", impl.Content);
        Assert.Contains("IMongoDbContext", impl.Content);
        Assert.Contains("IResiliencePipelineProvider", impl.Content);
    }

    [Fact]
    public void GetFiles_MongoDbExtensionsContainsResiliencePipeline()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("MongoDbExtensions.cs"));
        Assert.Contains("AddResiliencePipeline", ext.Content);
        Assert.Contains("AddRetry", ext.Content);
        Assert.Contains("AddCircuitBreaker", ext.Content);
    }

    [Fact]
    public void GetFiles_MongoDbExtensionsRegistersIMongoDbContext()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("MongoDbExtensions.cs"));
        Assert.Contains("IMongoDbContext", ext.Content);
        Assert.Contains("MongoDbContext", ext.Content);
        Assert.Contains("MyApp.Infra.MongoDb.Context", ext.Content);
    }

    [Fact]
    public void GetFiles_MongoDbExtensionsRegistersIMongoClient()
    {
        var files = MongoDbExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.MongoDb",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("MongoDbExtensions.cs"));
        Assert.Contains("IMongoClient", ext.Content);
        Assert.Contains("MongoClient", ext.Content);
    }
}
