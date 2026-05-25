using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.Redis;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;

namespace OpenBase.CLI.Tests.Commands;

public class RedisCacheExtensionHandlerTests
{
    private readonly Mock<IAnsiConsole> _console = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    public RedisCacheExtensionHandlerTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.Run(It.IsAny<string>())).Returns((true, string.Empty));
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
    }

    private RedisCacheExtensionHandler CreateHandler() =>
        new(_console.Object, _dotNetRunner.Object, _fileWriter.Object);

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
    public void Name_IsRedis()
    {
        Assert.Equal("redis", CreateHandler().Name);
    }

    [Fact]
    public void SupportedProviders_IsEmpty()
    {
        Assert.Empty(CreateHandler().SupportedProviders);
    }

    [Fact]
    public void Apply_CreatesICacheServiceFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("ICacheService.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesRedisCacheServiceFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("RedisCacheService.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesRedisExtensionsFile()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("RedisExtensions.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_FileExists_SkipsCreation()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs"))))
                   .Returns("AddRedisCache");

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("ICacheService.cs")),
            It.IsAny<string>()), Times.Never);
    }

    // --- Infra.Cache project creation ---

    [Fact]
    public void Apply_CreatesInfraCacheProject_WhenNotExists()
    {
        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("new classlib") && s.Contains("Infra.Cache"))),
            Times.Once);
    }

    [Fact]
    public void Apply_SkipsProjectCreation_WhenAlreadyExists()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns(true);

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("new classlib"))),
            Times.Never);
    }

    [Fact]
    public void Apply_PackageInstallFails_ReturnsError()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns("<Project />");
        _dotNetRunner.Setup(r => r.Run(It.Is<string>(s => s.Contains("package")))).Returns((false, "network error"));

        var result = CreateHandler().Apply(BuildContext());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Apply_PackageInstallFails_DoesNotGenerateFiles()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns("<Project />");
        _dotNetRunner.Setup(r => r.Run(It.Is<string>(s => s.Contains("package")))).Returns((false, "network error"));

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("RedisCacheService.cs")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_AddsInfraCacheToSolution_WhenSlnFound()
    {
        _fileWriter.Setup(f => f.FindSolutionFile(It.IsAny<string>())).Returns("/solution/MyApp.sln");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("sln") && s.Contains("add") && s.Contains("Infra.Cache.csproj"))),
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
    public void Apply_AddsInfraCacheReferenceToApplication()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") &&
                               s.Contains("Infra.Cache.csproj") &&
                               s.Contains("Application.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_AddsPresentationReferenceToInfraCache()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") &&
                               s.Contains("Presentation.Api.csproj") &&
                               s.Contains("Infra.Cache.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InstallsCachingPackageInInfraCache()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.Cache.csproj") &&
                               s.Contains("Microsoft.Extensions.Caching.StackExchangeRedis"))),
            Times.Once);
    }

    [Fact]
    public void Apply_InstallsResiliencePackageInInfraCache()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Cache.csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.Cache.csproj") &&
                               s.Contains("Microsoft.Extensions.Resilience"))),
            Times.Once);
    }

    // --- Program.cs injection ---

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
                c.Contains("AddRedisCache") &&
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
    public void Apply_ProgramCs_AddRedisCacheBeforeBuild()
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
        var addIdx   = written!.IndexOf("AddRedisCache", StringComparison.Ordinal);
        var buildIdx = written.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(addIdx < buildIdx, "AddRedisCache should appear before builder.Build()");
    }

    [Fact]
    public void Apply_ProgramCsAlreadyConfigured_SkipsWrite()
    {
        const string alreadyDone = """
            using MyApp.Presentation.Api.Extensions;
            builder.Services.AddRedisCache(builder.Configuration);
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
    public void Apply_InjectsRedis_IntoAppSettingsJson_WhenFileExists()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.Is<string>(c => c.Contains("Redis") && c.Contains("ConnectionString") && c.Contains("InstanceName"))),
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
    public void Apply_InjectsRedis_IntoDevelopmentJson_WhenFileExists()
    {
        const string appSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.Development.json")))).Returns(appSettings);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.Development.json")),
            It.Is<string>(c => c.Contains("Redis") && c.Contains("ConnectionString") && c.Contains("InstanceName"))),
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
            It.Is<string>(c => c.Contains("Redis"))), Times.Once);
        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.Development.json")),
            It.Is<string>(c => c.Contains("Redis"))), Times.Once);
    }

    [Fact]
    public void Apply_AppSettingsAlreadyHasRedis_SkipsWrite()
    {
        const string appSettings = """{"Redis":{"ConnectionString":"localhost:6379","InstanceName":"openbase_"}}""";
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
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFiles_ICacheServiceInApplicationLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Application") && f.Path.EndsWith("ICacheService.cs"));
    }

    [Fact]
    public void GetFiles_RedisCacheServiceInInfraCacheLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Infra.Cache") && f.Path.EndsWith("RedisCacheService.cs"));
    }

    [Fact]
    public void GetFiles_RedisExtensionsInPresentationLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Presentation.Api") && f.Path.EndsWith("RedisExtensions.cs"));
    }

    [Fact]
    public void GetFiles_ICacheServiceContentContainsNamespace()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "AcmeCorp",
            "/solution/src/AcmeCorp.Application",
            "/solution/src/AcmeCorp.Infra.Cache",
            "/solution/src/AcmeCorp.Presentation.Api").ToList();

        var iface = files.First(f => f.Path.EndsWith("ICacheService.cs"));
        Assert.Contains("AcmeCorp.Application.Interfaces.Services", iface.Content);
    }

    [Fact]
    public void GetFiles_ICacheServiceContentContainsMethods()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var iface = files.First(f => f.Path.EndsWith("ICacheService.cs"));
        Assert.Contains("GetAsync", iface.Content);
        Assert.Contains("SetAsync", iface.Content);
        Assert.Contains("RemoveAsync", iface.Content);
        Assert.Contains("ExistsAsync", iface.Content);
    }

    [Fact]
    public void GetFiles_RedisCacheServiceImplementsICacheService()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var impl = files.First(f => f.Path.EndsWith("RedisCacheService.cs"));
        Assert.Contains("ICacheService", impl.Content);
        Assert.Contains("IDistributedCache", impl.Content);
        Assert.Contains("IResiliencePipelineProvider", impl.Content);
    }

    [Fact]
    public void GetFiles_RedisCacheServiceUsesInfraCacheNamespace()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var impl = files.First(f => f.Path.EndsWith("RedisCacheService.cs"));
        Assert.Contains("MyApp.Infra.Cache.Services", impl.Content);
    }

    [Fact]
    public void GetFiles_RedisExtensionsContentContainsAddStackExchangeRedisCache()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("RedisExtensions.cs"));
        Assert.Contains("AddStackExchangeRedisCache", ext.Content);
    }

    [Fact]
    public void GetFiles_RedisExtensionsContainsResiliencePipeline()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("RedisExtensions.cs"));
        Assert.Contains("AddResiliencePipeline", ext.Content);
        Assert.Contains("AddRetry", ext.Content);
        Assert.Contains("AddCircuitBreaker", ext.Content);
    }

    [Fact]
    public void GetFiles_RedisExtensionsRegistersICacheService()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Cache",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("RedisExtensions.cs"));
        Assert.Contains("ICacheService", ext.Content);
        Assert.Contains("RedisCacheService", ext.Content);
        Assert.Contains("MyApp.Infra.Cache.Services", ext.Content);
    }
}
