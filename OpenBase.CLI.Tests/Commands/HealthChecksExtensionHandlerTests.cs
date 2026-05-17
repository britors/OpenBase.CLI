using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.HealthChecks;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Commands;

public class HealthChecksExtensionHandlerTests
{
    private readonly Mock<IAnsiConsole> _console = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IFileWriter> _fileWriter = new();
    private readonly Mock<IExtensionRegistry> _extensionRegistry = new();

    public HealthChecksExtensionHandlerTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.Run(It.IsAny<string>())).Returns((true, string.Empty));
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
        _extensionRegistry.Setup(r => r.GetAll(It.IsAny<string>())).Returns([]);
    }

    private HealthChecksExtensionHandler CreateHandler() =>
        new(_console.Object, _dotNetRunner.Object, _fileWriter.Object, _extensionRegistry.Object);

    private static ExtensionContext BuildContext(string? solutionDir = "/solution", string? ns = "MyApp") =>
        new(null, solutionDir ?? "/solution", null, [], solutionDir, ns);

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
    public void Apply_NameIs_healthchecks()
    {
        Assert.Equal("healthchecks", CreateHandler().Name);
    }

    [Fact]
    public void Apply_HasNoSupportedProviders()
    {
        Assert.Empty(CreateHandler().SupportedProviders);
    }

    [Fact]
    public void Apply_CreatesHealthChecksExtensionsFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("HealthChecksExtensions.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_FileExists_SkipsCreation()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("HealthChecksExtensions.cs")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_AddsCoreUiPackagesToPresentationApi()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Presentation.Api.csproj") && s.Contains("AspNetCore.HealthChecks.UI.Client"))),
            Times.Once);
    }

    [Fact]
    public void Apply_SqlServerDetected_AddsSqlServerPackage()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Data.csproj"))))
                   .Returns("<PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Presentation.Api.csproj"))))
                   .Returns("<Project />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.SqlServer"))),
            Times.Once);
    }

    [Fact]
    public void Apply_PostgresDetected_AddsNpgSqlPackage()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Data.csproj"))))
                   .Returns("<PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Presentation.Api.csproj"))))
                   .Returns("<Project />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.NpgSql"))),
            Times.Once);
    }

    [Fact]
    public void Apply_RedisExtensionInstalled_AddsRedisPackage()
    {
        _extensionRegistry.Setup(r => r.GetAll(It.IsAny<string>()))
                          .Returns([new ExtensionEntry("redis", null, DateTimeOffset.UtcNow)]);
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.Redis"))),
            Times.Once);
    }

    [Fact]
    public void Apply_RabbitMqExtensionInstalled_AddsRabbitMqPackage()
    {
        _extensionRegistry.Setup(r => r.GetAll(It.IsAny<string>()))
                          .Returns([new ExtensionEntry("rabbitmq", null, DateTimeOffset.UtcNow)]);
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.RabbitMQ"))),
            Times.Once);
    }

    [Fact]
    public void Apply_OracleDetected_AddsOraclePackage()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Data.csproj"))))
                   .Returns("<PackageReference Include=\"Oracle.EntityFrameworkCore\" />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Presentation.Api.csproj"))))
                   .Returns("<Project />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddOpenBaseHealthChecks\nMapOpenBaseHealthChecks");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.Oracle"))),
            Times.Once);
    }

    [Fact]
    public void Apply_NoServicesDetected_SkipsConditionalPackages()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("AspNetCore.HealthChecks.SqlServer") ||
                               s.Contains("AspNetCore.HealthChecks.NpgSql") ||
                               s.Contains("AspNetCore.HealthChecks.Oracle") ||
                               s.Contains("AspNetCore.HealthChecks.Redis") ||
                               s.Contains("AspNetCore.HealthChecks.RabbitMQ"))),
            Times.Never);
    }

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
                c.Contains("AddOpenBaseHealthChecks") &&
                c.Contains("MapOpenBaseHealthChecks") &&
                c.Contains("using MyApp.Presentation.Api.Extensions;"))),
            Times.Once);
    }

    [Fact]
    public void Apply_ProgramCs_InjectsUsingDirectiveAtTopOfFile()
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
        Assert.StartsWith("using MyApp.Presentation.Api.Extensions;", written);
    }

    [Fact]
    public void Apply_ProgramCs_AddHealthChecksBeforeBuild()
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
        var addIdx = written!.IndexOf("AddOpenBaseHealthChecks", StringComparison.Ordinal);
        var buildIdx = written.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(addIdx < buildIdx, "AddOpenBaseHealthChecks should appear before builder.Build()");
    }

    [Fact]
    public void Apply_ProgramCs_MapHealthChecksBeforeMapControllers()
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
        var mapHcIdx = written!.IndexOf("MapOpenBaseHealthChecks", StringComparison.Ordinal);
        var mapCtrlIdx = written.IndexOf("app.MapControllers();", StringComparison.Ordinal);
        Assert.True(mapHcIdx < mapCtrlIdx, "MapOpenBaseHealthChecks should appear before MapControllers");
    }

    [Fact]
    public void Apply_ProgramCsAlreadyConfigured_SkipsWrite()
    {
        const string alreadyDone = """
            using MyApp.Presentation.Api.Extensions;
            builder.Services.AddOpenBaseHealthChecks(builder.Configuration);
            var app = builder.Build();
            app.MapOpenBaseHealthChecks();
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

    [Fact]
    public void GetFiles_ReturnsOneFile()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.Single(files);
    }

    [Fact]
    public void GetFiles_HealthChecksExtensionsInPresentationLayer()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Presentation.Api") && f.Path.EndsWith("HealthChecksExtensions.cs"));
    }

    [Fact]
    public void GetFiles_ContentContainsNamespace()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "AcmeCorp",
            "/solution/src/AcmeCorp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.Contains("AcmeCorp.Presentation.Api.Extensions", files[0].Content);
    }

    [Fact]
    public void GetFiles_ContainsHealthEndpoint()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.Contains("\"/health\"", files[0].Content);
    }

    [Fact]
    public void GetFiles_ContainsReadyEndpoint()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.Contains("\"/health/ready\"", files[0].Content);
    }

    [Fact]
    public void GetFiles_SqlServerDetected_ContentContainsAddSqlServer()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(HasSqlServer: true, HasPostgres: false, HasOracle: false, HasRedis: false, HasRabbitMq: false)).ToList();

        Assert.Contains("AddSqlServer", files[0].Content);
        Assert.Contains("OpenBaseSQLServer", files[0].Content);
    }

    [Fact]
    public void GetFiles_PostgresDetected_ContentContainsAddNpgSql()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(HasSqlServer: false, HasPostgres: true, HasOracle: false, HasRedis: false, HasRabbitMq: false)).ToList();

        Assert.Contains("AddNpgSql", files[0].Content);
        Assert.Contains("OpenBasePostgres", files[0].Content);
    }

    [Fact]
    public void GetFiles_OracleDetected_ContentContainsAddOracle()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(HasSqlServer: false, HasPostgres: false, HasOracle: true, HasRedis: false, HasRabbitMq: false)).ToList();

        Assert.Contains("AddOracle", files[0].Content);
        Assert.Contains("OpenBaseOracle", files[0].Content);
    }

    [Fact]
    public void GetFiles_RedisDetected_ContentContainsAddRedis()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(HasSqlServer: false, HasPostgres: false, HasOracle: false, HasRedis: true, HasRabbitMq: false)).ToList();

        Assert.Contains("AddRedis", files[0].Content);
    }

    [Fact]
    public void GetFiles_RabbitMqDetected_ContentContainsAddRabbitMQ()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(HasSqlServer: false, HasPostgres: false, HasOracle: false, HasRedis: false, HasRabbitMq: true)).ToList();

        Assert.Contains("AddRabbitMQ", files[0].Content);
    }

    [Fact]
    public void GetFiles_NoServicesDetected_ContentDoesNotContainConditionalChecks()
    {
        var files = HealthChecksExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Presentation.Api",
            new DetectedServices(false, false, false, false, false)).ToList();

        Assert.DoesNotContain("AddSqlServer", files[0].Content);
        Assert.DoesNotContain("AddNpgSql", files[0].Content);
        Assert.DoesNotContain("AddOracle", files[0].Content);
        Assert.DoesNotContain("AddRedis", files[0].Content);
        Assert.DoesNotContain("AddRabbitMQ", files[0].Content);
    }
}
