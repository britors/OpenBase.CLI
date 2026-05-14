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
    }

    private JwtExtensionHandler CreateHandler() =>
        new(_console.Object, _dotNetRunner.Object, _fileWriter.Object);

    private static ExtensionContext BuildContext(string? solutionDir = "/solution", string? ns = "MyApp") =>
        new(null, solutionDir ?? "/solution", null, [])
        {
            SolutionDir = solutionDir,
            RootNamespace = ns
        };

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
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".cs")))).Returns(true);

        CreateHandler().Apply(BuildContext());

        _fileWriter.Verify(f => f.WriteAllText(
            It.Is<string>(p => p.EndsWith(".cs")),
            It.IsAny<string>()), Times.Never);
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
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith(".csproj"))))
                   .Returns("<PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" />");

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
}
