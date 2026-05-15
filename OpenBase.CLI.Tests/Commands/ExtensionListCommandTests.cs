using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.HealthChecks;
using OpenBase.CLI.Commands.Extension.Jwt;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class ExtensionListCommandTests
{
    private readonly Mock<ICsprojLocator> _csprojLocator = new();
    private readonly Mock<IProjectLocator> _projectLocator = new();
    private readonly Mock<IExtensionRegistry> _registry = new();

    private static readonly IExtensionHandler[] AllHandlers =
    [
        new JwtExtensionHandler(
            new Mock<IAnsiConsole>().Object,
            new Mock<IDotNetRunner>().Object,
            new Mock<IFileWriter>().Object),
        new HealthChecksExtensionHandler(
            new Mock<IAnsiConsole>().Object,
            new Mock<IDotNetRunner>().Object,
            new Mock<IFileWriter>().Object,
            new Mock<IExtensionRegistry>().Object),
    ];

    public ExtensionListCommandTests()
    {
        SR.Configure();
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), It.IsAny<string?>()))
                       .Returns((null, null));
        _csprojLocator.Setup(c => c.Find(It.IsAny<string>())).Returns((string?)null);
        _registry.Setup(r => r.GetAll(It.IsAny<string>())).Returns([]);
    }

    private Task<int> Run(IEnumerable<IExtensionHandler>? handlers = null)
    {
        var cmd = new ExtensionListCommand(
            CommandTestHelper.CreateConsole(),
            _csprojLocator.Object,
            _projectLocator.Object,
            _registry.Object,
            handlers ?? AllHandlers);

        return ((ICommand<ExtensionListSettings>)cmd)
            .ExecuteAsync(CommandTestHelper.CreateContext("list"), new ExtensionListSettings(), CancellationToken.None);
    }

    [Fact]
    public async Task Execute_ReturnsZero()
    {
        var result = await Run();
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_NoProjectDetected_DoesNotQueryRegistry()
    {
        await Run();
        _registry.Verify(r => r.GetAll(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_SolutionDetected_QueriesRegistryWithSolutionDir()
    {
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), It.IsAny<string?>()))
                       .Returns(("/my/solution", "MyApp"));

        await Run();

        _registry.Verify(r => r.GetAll("/my/solution"), Times.Once);
    }

    [Fact]
    public async Task Execute_CsprojFallback_QueriesRegistryWithProjectDir()
    {
        _csprojLocator.Setup(c => c.Find(It.IsAny<string>())).Returns("/proj/MyApp.csproj");

        await Run();

        _registry.Verify(r => r.GetAll("/proj"), Times.Once);
    }

    [Fact]
    public async Task Execute_InstalledExtension_QueriesRegistry()
    {
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), It.IsAny<string?>()))
                       .Returns(("/solution", "MyApp"));
        _registry.Setup(r => r.GetAll("/solution"))
                 .Returns([new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow)]);

        await Run();

        _registry.Verify(r => r.GetAll("/solution"), Times.Once);
    }

    [Fact]
    public async Task Execute_NoHandlers_ReturnsZero()
    {
        var result = await Run([]);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => Run());
        Assert.Null(ex);
    }
}
