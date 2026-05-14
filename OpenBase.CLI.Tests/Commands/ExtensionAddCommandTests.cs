using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class ExtensionAddCommandTests
{
    private readonly Mock<ICsprojLocator> _csprojLocator = new();
    private readonly Mock<ICsprojPackageReader> _packageReader = new();
    private readonly Mock<IProjectLocator> _projectLocator = new();
    private readonly Mock<IExtensionRegistry> _registry = new();
    private readonly Mock<IExtensionHandler> _handler = new();

    public ExtensionAddCommandTests()
    {
        SR.Configure();
        _packageReader.Setup(r => r.ReadPackages(It.IsAny<string>())).Returns([]);
        _registry.Setup(r => r.IsInstalled(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                 .Returns(false);
        // Default: no OpenBase solution found, fall back to csproj detection
        _projectLocator.Setup(l => l.Detect(It.IsAny<string>(), It.IsAny<string?>()))
                       .Returns(((string?)null, (string?)null));
    }

    private ExtensionAddCommand CreateCommand(params IExtensionHandler[] handlers) =>
        new(CommandTestHelper.CreateConsole(), _csprojLocator.Object, _packageReader.Object,
            _projectLocator.Object, _registry.Object, handlers);

    private static ExtensionAddSettings BuildSettings(string name, string? provider = null) =>
        new() { Name = name, Provider = provider };

    private Task<int> Run(ExtensionAddSettings settings, params IExtensionHandler[] handlers) =>
        ((ICommand<ExtensionAddSettings>)CreateCommand(handlers))
            .ExecuteAsync(CommandTestHelper.CreateContext("add"), settings, CancellationToken.None);

    [Fact]
    public async Task Execute_NoCsprojAndNoSolution_ReturnsOne()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns((string?)null);

        var result = await Run(BuildSettings("jwt"));

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Execute_WithSolutionDir_SkipsCsprojLocator()
    {
        _projectLocator.Setup(l => l.Detect(It.IsAny<string>(), null))
                       .Returns(("/solution", "MyApp"));
        _handler.Setup(h => h.Name).Returns("jwt");
        _handler.Setup(h => h.SupportedProviders).Returns([]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(true));

        var result = await Run(BuildSettings("jwt"), _handler.Object);

        Assert.Equal(0, result);
        _csprojLocator.Verify(l => l.Find(It.IsAny<string>()), Times.Never);
        _handler.Verify(h => h.Apply(It.Is<ExtensionContext>(c =>
            c.SolutionDir == "/solution" && c.RootNamespace == "MyApp")), Times.Once);
    }

    [Fact]
    public async Task Execute_ExtensionAlreadyInstalled_ReturnsZero()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _registry.Setup(r => r.IsInstalled("/proj", "jwt", null)).Returns(true);

        var result = await Run(BuildSettings("jwt"));

        Assert.Equal(0, result);
        _registry.Verify(r => r.Register(It.IsAny<string>(), It.IsAny<ExtensionEntry>()), Times.Never);
    }

    [Fact]
    public async Task Execute_UnknownExtension_ReturnsOne()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");

        var result = await Run(BuildSettings("unknown"));

        Assert.Equal(1, result);
        _registry.Verify(r => r.Register(It.IsAny<string>(), It.IsAny<ExtensionEntry>()), Times.Never);
    }

    [Fact]
    public async Task Execute_InvalidProvider_ReturnsOne()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _handler.Setup(h => h.Name).Returns("cache");
        _handler.Setup(h => h.SupportedProviders).Returns(["redis", "memory"]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(true));

        var result = await Run(BuildSettings("cache", "azure"), _handler.Object);

        Assert.Equal(1, result);
        _registry.Verify(r => r.Register(It.IsAny<string>(), It.IsAny<ExtensionEntry>()), Times.Never);
    }

    [Fact]
    public async Task Execute_HandlerFails_ReturnsOne()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _handler.Setup(h => h.Name).Returns("jwt");
        _handler.Setup(h => h.SupportedProviders).Returns([]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(false, "something went wrong"));

        var result = await Run(BuildSettings("jwt"), _handler.Object);

        Assert.Equal(1, result);
        _registry.Verify(r => r.Register(It.IsAny<string>(), It.IsAny<ExtensionEntry>()), Times.Never);
    }

    [Fact]
    public async Task Execute_Success_RegistersExtensionAndReturnsZero()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _handler.Setup(h => h.Name).Returns("jwt");
        _handler.Setup(h => h.SupportedProviders).Returns([]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(true));

        var result = await Run(BuildSettings("jwt"), _handler.Object);

        Assert.Equal(0, result);
        _registry.Verify(r => r.Register("/proj",
            It.Is<ExtensionEntry>(e => e.Name == "jwt" && e.Provider == null)), Times.Once);
    }

    [Fact]
    public async Task Execute_ValidProvider_PassesProviderToHandler()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _handler.Setup(h => h.Name).Returns("cache");
        _handler.Setup(h => h.SupportedProviders).Returns(["redis", "memory"]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(true));

        var result = await Run(BuildSettings("cache", "redis"), _handler.Object);

        Assert.Equal(0, result);
        _handler.Verify(h => h.Apply(It.Is<ExtensionContext>(c => c.Provider == "redis")), Times.Once);
        _registry.Verify(r => r.Register("/proj",
            It.Is<ExtensionEntry>(e => e.Name == "cache" && e.Provider == "redis")), Times.Once);
    }

    [Fact]
    public async Task Execute_PassesInstalledPackagesToHandler()
    {
        _csprojLocator.Setup(l => l.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");
        _packageReader.Setup(r => r.ReadPackages("/proj/MyApp.csproj"))
                      .Returns(["Microsoft.AspNetCore.Authentication.JwtBearer"]);
        _handler.Setup(h => h.Name).Returns("jwt");
        _handler.Setup(h => h.SupportedProviders).Returns([]);
        _handler.Setup(h => h.Apply(It.IsAny<ExtensionContext>()))
                .Returns(new ExtensionApplyResult(true));

        await Run(BuildSettings("jwt"), _handler.Object);

        _handler.Verify(h => h.Apply(It.Is<ExtensionContext>(c =>
            c.InstalledPackages.Contains("Microsoft.AspNetCore.Authentication.JwtBearer"))), Times.Once);
    }
}
