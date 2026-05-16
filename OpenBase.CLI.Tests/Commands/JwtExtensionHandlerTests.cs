using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.Jwt;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;

namespace OpenBase.CLI.Tests.Commands;

public class JwtExtensionHandlerTests
{
    private readonly Mock<IAnsiConsole> _console = new();
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    public JwtExtensionHandlerTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.Run(It.IsAny<string>())).Returns((true, string.Empty));
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
        _fileWriter.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns([]);
    }

    private JwtExtensionHandler CreateHandler() =>
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
    public void Apply_CreatesITokenService()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("ITokenService.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesTokenService()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("TokenService.cs") && !p.EndsWith("ITokenService.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_CreatesJwtExtensions()
    {
        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("JwtExtensions.cs")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Apply_FilesExist_SkipsCreation()
    {
        const string alreadyConfigured = """
            using MyApp.Presentation.Api.Extensions;
            builder.Services.AddJwtAuthentication(builder.Configuration);
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(alreadyConfigured);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith(".cs")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_AddsApplicationReferenceToInfraData()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.Data.csproj") && s.Contains("reference") && s.Contains("Application.csproj"))),
            Times.Once);
    }

    [Fact]
    public void Apply_ApplicationReferenceAlreadyPresent_SkipsAdd()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Infra.Data.csproj"))))
                   .Returns("<ProjectReference Include=\"..\\MyApp.Application\\MyApp.Application.csproj\" />");
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => !p.EndsWith("Infra.Data.csproj") && p.EndsWith(".csproj"))))
                   .Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.Data.csproj") && s.Contains("reference"))),
            Times.Never);
    }

    [Fact]
    public void Apply_AddsNuGetPackageToInfraData()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Infra.Data.csproj") && s.Contains("package Microsoft.AspNetCore.Authentication.JwtBearer"))),
            Times.Once);
    }

    [Fact]
    public void Apply_AddsNuGetPackageToPresentationApi()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj")))).Returns("<Project />");

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(
            It.Is<string>(s => s.Contains("Presentation.Api.csproj") && s.Contains("package Microsoft.AspNetCore.Authentication.JwtBearer"))),
            Times.Once);
    }

    [Fact]
    public void Apply_PackageAlreadyInCsproj_SkipsAdd()
    {
        const string alreadyConfigured =
            "<PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" />" +
            "<ProjectReference Include=\"..\\MyApp.Application\\MyApp.Application.csproj\" />";

        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj"))))
                   .Returns(alreadyConfigured);

        CreateHandler().Apply(BuildContext());

        _dotNetRunner.Verify(r => r.Run(It.Is<string>(s => s.Contains("add"))), Times.Never);
    }

    [Fact]
    public void Apply_InjectsJwtSectionIntoAppSettings()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json"))))
                   .Returns("{}");

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.Is<string>(content => content.Contains("Jwt"))), Times.Once);
    }

    [Fact]
    public void Apply_AppSettingsAlreadyHasJwt_SkipsInjection()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("appsettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("appsettings.json"))))
                   .Returns("{\"Jwt\": {\"Secret\": \"existing\"}}");

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("appsettings.json")),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetFiles_ReturnsThreeFiles()
    {
        var files = JwtExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFiles_ITokenServiceInApplicationLayer()
    {
        var files = JwtExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Application") && f.Path.EndsWith("ITokenService.cs"));
    }

    [Fact]
    public void GetFiles_TokenServiceInInfraDataLayer()
    {
        var files = JwtExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Infra.Data") && f.Path.EndsWith("TokenService.cs"));
    }

    [Fact]
    public void GetFiles_JwtExtensionsInPresentationLayer()
    {
        var files = JwtExtensionHandler.GetFiles("MyApp",
            "/solution/src/MyApp.Application",
            "/solution/src/MyApp.Infra.Data",
            "/solution/src/MyApp.Presentation.Api").ToList();

        Assert.Contains(files, f =>
            f.Path.Contains("MyApp.Presentation.Api") && f.Path.EndsWith("JwtExtensions.cs"));
    }

    [Fact]
    public void GetFiles_TokenServiceContainsNamespace()
    {
        var files = JwtExtensionHandler.GetFiles("AcmeCorp",
            "/s/AcmeCorp.Application",
            "/s/AcmeCorp.Infra.Data",
            "/s/AcmeCorp.Presentation.Api").ToList();

        var tokenService = files.First(f => f.Path.EndsWith("TokenService.cs") && !f.Path.EndsWith("ITokenService.cs"));
        Assert.Contains("AcmeCorp.Infra.Data.Services", tokenService.Content);
        Assert.Contains("AcmeCorp.Application.Interfaces.Services", tokenService.Content);
    }

    [Fact]
    public void Apply_NameIs_jwt()
    {
        Assert.Equal("jwt", CreateHandler().Name);
    }

    [Fact]
    public void Apply_HasNoSupportedProviders()
    {
        Assert.Empty(CreateHandler().SupportedProviders);
    }

    private const string MinimalProgramCs = """
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        var app = builder.Build();
        app.UseHttpsRedirection();
        app.MapControllers();
        await app.RunAsync();
        """;

    [Fact]
    public void Apply_InjectsProgramCs_WhenFileExists()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(MinimalProgramCs);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith("Program.cs")),
            It.Is<string>(c =>
                c.Contains("using MyApp.Presentation.Api.Extensions;") &&
                c.Contains("AddJwtAuthentication") &&
                c.Contains("app.UseAuthentication();") &&
                c.Contains("app.UseAuthorization();"))),
            Times.Once);
    }

    [Fact]
    public void Apply_ProgramCs_AddJwtBeforeBuild()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(MinimalProgramCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        var addJwtIdx = written!.IndexOf("AddJwtAuthentication", StringComparison.Ordinal);
        var buildIdx = written.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(addJwtIdx < buildIdx, "AddJwtAuthentication should appear before builder.Build()");
    }

    [Fact]
    public void Apply_ProgramCs_UseAuthBeforeMapControllers()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(MinimalProgramCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        var authIdx = written!.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var mapIdx = written.IndexOf("app.MapControllers();", StringComparison.Ordinal);
        Assert.True(authIdx < mapIdx, "UseAuthentication should appear before MapControllers");
    }

    [Fact]
    public void Apply_ProgramCs_AlreadyHasUseAuthentication_OnlyAddsUseAuthorization()
    {
        const string withAuth = """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.UseAuthentication();
            app.MapControllers();
            await app.RunAsync();
            """;

        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(withAuth);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        var authIdx = written!.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var authzIdx = written.IndexOf("app.UseAuthorization();", StringComparison.Ordinal);
        Assert.True(authIdx < authzIdx, "UseAuthorization should be inserted after UseAuthentication");
        Assert.Equal(1, CountOccurrences(written, "app.UseAuthentication();"));
    }

    [Fact]
    public void Apply_ProgramCs_AddsUsingDirective()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("Program.cs")))).Returns(MinimalProgramCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(It.Is<string>(p => p.EndsWith("Program.cs")), It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        Assert.Contains("using MyApp.Presentation.Api.Extensions;", written);
    }

    [Fact]
    public void Apply_ProgramCs_AlreadyFullyConfigured_SkipsWrite()
    {
        const string alreadyDone = """
            using MyApp.Presentation.Api.Extensions;
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddJwtAuthentication(builder.Configuration);
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
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

    private const string MinimalControllerCs = """
        using Microsoft.AspNetCore.Mvc;

        namespace MyApp.Presentation.Api.Controllers;

        [ApiController]
        [Route("api/product")]
        [Produces("application/json")]
        public class ProductController : ControllerBase
        {
        }
        """;

    [Fact]
    public void Apply_ProtectsExistingController_AddsAuthorizeAttribute()
    {
        const string controllerPath = "/solution/src/MyApp.Presentation.Api/Controllers/ProductController.cs";
        _fileWriter.Setup(f => f.GetFiles(It.Is<string>(p => p.EndsWith("Controllers")), It.IsAny<string>()))
                   .Returns([controllerPath]);
        _fileWriter.Setup(f => f.ReadAllText(controllerPath)).Returns(MinimalControllerCs);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            controllerPath,
            It.Is<string>(c => c.Contains("[Authorize]"))), Times.Once);
    }

    [Fact]
    public void Apply_ProtectsExistingController_AddsAuthorizationUsing()
    {
        const string controllerPath = "/solution/src/MyApp.Presentation.Api/Controllers/ProductController.cs";
        _fileWriter.Setup(f => f.GetFiles(It.Is<string>(p => p.EndsWith("Controllers")), It.IsAny<string>()))
                   .Returns([controllerPath]);
        _fileWriter.Setup(f => f.ReadAllText(controllerPath)).Returns(MinimalControllerCs);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            controllerPath,
            It.Is<string>(c => c.Contains("using Microsoft.AspNetCore.Authorization;"))), Times.Once);
    }

    [Fact]
    public void Apply_ProtectsExistingController_AuthorizeAfterApiController()
    {
        const string controllerPath = "/solution/src/MyApp.Presentation.Api/Controllers/ProductController.cs";
        _fileWriter.Setup(f => f.GetFiles(It.Is<string>(p => p.EndsWith("Controllers")), It.IsAny<string>()))
                   .Returns([controllerPath]);
        _fileWriter.Setup(f => f.ReadAllText(controllerPath)).Returns(MinimalControllerCs);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(controllerPath, It.IsAny<string>()))
                   .Callback<string, string>((_, c) => written = c);

        CreateHandler().Apply(BuildContext());

        Assert.NotNull(written);
        var apiIdx = written!.IndexOf("[ApiController]", StringComparison.Ordinal);
        var authIdx = written.IndexOf("[Authorize]", StringComparison.Ordinal);
        Assert.True(apiIdx < authIdx, "[Authorize] should appear after [ApiController]");
    }

    [Fact]
    public void Apply_ControllerAlreadyHasAuthorize_SkipsProtection()
    {
        const string alreadyProtected = """
            using Microsoft.AspNetCore.Authorization;
            using Microsoft.AspNetCore.Mvc;

            [ApiController]
            [Authorize]
            [Route("api/product")]
            public class ProductController : ControllerBase { }
            """;
        const string controllerPath = "/solution/src/MyApp.Presentation.Api/Controllers/ProductController.cs";
        _fileWriter.Setup(f => f.GetFiles(It.Is<string>(p => p.EndsWith("Controllers")), It.IsAny<string>()))
                   .Returns([controllerPath]);
        _fileWriter.Setup(f => f.ReadAllText(controllerPath)).Returns(alreadyProtected);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(controllerPath, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Apply_NoControllers_DoesNotThrow()
    {
        _fileWriter.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns([]);

        var ex = Record.Exception(() => CreateHandler().Apply(BuildContext()));

        Assert.Null(ex);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var idx = 0;
        while ((idx = source.IndexOf(value, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += value.Length;
        }
        return count;
    }
}
