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

    [Fact]
    public void Apply_AddsProjectReference_WhenInfraDataMissingApplicationRef()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") &&
                               s.Contains("Infra.Data.csproj") &&
                               s.Contains("Application.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_SkipsProjectReference_WhenAlreadyPresent()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Data.csproj"))))
                   .Returns("<ProjectReference Include=\"..\\MyApp.Application\\MyApp.Application.csproj\" />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => !p.EndsWith("Infra.Data.csproj") && p.EndsWith(".csproj"))))
                   .Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("reference") && s.Contains("Application.csproj"))),
            Times.Never);
    }

    [Fact]
    public void Apply_AddsCachingPackageToPresentationApi()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Presentation.Api.csproj") &&
                               s.Contains("Microsoft.Extensions.Caching.StackExchangeRedis"))),
            Times.Once);
    }

    [Fact]
    public void Apply_PackageAlreadyPresent_SkipsInstall()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj"))))
                   .Returns("<PackageReference Include=\"Microsoft.Extensions.Caching.StackExchangeRedis\" />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Microsoft.Extensions.Caching.StackExchangeRedis"))),
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
        var addIdx = written!.IndexOf("AddRedisCache", StringComparison.Ordinal);
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

    [Fact]
    public void GetFiles_ReturnsThreeFiles()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFiles_ICacheServiceInApplicationLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Application") && f.Path.EndsWith("ICacheService.cs"));
    }

    [Fact]
    public void GetFiles_RedisCacheServiceInInfraDataLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Infra.Data") && f.Path.EndsWith("RedisCacheService.cs"));
    }

    [Fact]
    public void GetFiles_RedisExtensionsInPresentationLayer()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
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
            "/solution/src/AcmeCorp.Infra.Data",
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
            "/solution/src/MyApp.Infra.Data",
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
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var impl = files.First(f => f.Path.EndsWith("RedisCacheService.cs"));
        Assert.Contains("ICacheService", impl.Content);
        Assert.Contains("IDistributedCache", impl.Content);
    }

    [Fact]
    public void GetFiles_RedisExtensionsContentContainsAddStackExchangeRedisCache()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("RedisExtensions.cs"));
        Assert.Contains("AddStackExchangeRedisCache", ext.Content);
    }

    [Fact]
    public void GetFiles_RedisExtensionsRegistersICacheService()
    {
        var files = RedisCacheExtensionHandler.GetFiles(
            "MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        var ext = files.First(f => f.Path.EndsWith("RedisExtensions.cs"));
        Assert.Contains("ICacheService", ext.Content);
        Assert.Contains("RedisCacheService", ext.Content);
    }
}
