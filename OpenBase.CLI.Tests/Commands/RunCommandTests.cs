using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class RunCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IProjectLocator> _projectLocator = new();
    private readonly Mock<ICsprojLocator> _csprojLocator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();
    private readonly Mock<IBrowserLauncher> _browserLauncher = new();

    public RunCommandTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, string.Empty));
        _dotNetRunner.Setup(r => r.RunLiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(0);
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), null))
                       .Returns(("/solution", "MyApp"));
        _fileWriter.Setup(f => f.FindSolutionFile("/solution"))
                   .Returns("/solution/MyApp.sln");
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(true);
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("launchSettings.json")))).Returns(false);
    }

    private RunCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _dotNetRunner.Object,
            _projectLocator.Object, _csprojLocator.Object, _fileWriter.Object, _browserLauncher.Object);

    private static Task<int> Execute(RunCommand cmd, RunSettings? settings = null) =>
        ((ICommand<RunSettings>)cmd).ExecuteAsync(
            CommandTestHelper.CreateContext("run"), settings ?? new RunSettings(), CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_ValidProject_ReturnsZero()
    {
        var result = await Execute(CreateCommand());

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ProjectLocatorReturnsNull_ReturnsOne()
    {
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), null)).Returns((null, null));

        var result = await Execute(CreateCommand());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PresentationCsprojNotFound_ReturnsOne()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith(".csproj")))).Returns(false);

        var result = await Execute(CreateCommand());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_BuildFails_ReturnsOne()
    {
        _dotNetRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((false, "build error"));

        var result = await Execute(CreateCommand());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_NoBuild_SkipsBuildSteps()
    {
        await Execute(CreateCommand(), new RunSettings { NoBuild = true });

        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CallsRunLive_WithPresentationCsproj()
    {
        await Execute(CreateCommand(), new RunSettings { NoBuild = true });

        _dotNetRunner.Verify(r => r.RunLiveAsync(
            It.Is<string>(s => s.Contains("MyApp.Presentation.Api.csproj")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RunLiveReturnsNonZero_ReturnsSameCode()
    {
        _dotNetRunner.Setup(r => r.RunLiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(42);

        var result = await Execute(CreateCommand(), new RunSettings { NoBuild = true });

        Assert.Equal(42, result);
    }

    [Fact]
    public void ResolveSwaggerUrl_WithLaunchSettings_ReturnsHttpsUrl()
    {
        const string launchSettings = """
            {
              "profiles": {
                "MyApp": {
                  "commandName": "Project",
                  "applicationUrl": "https://localhost:7100;http://localhost:5100"
                }
              }
            }
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("launchSettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("launchSettings.json"))))
                   .Returns(launchSettings);

        var cmd = CreateCommand();
        var url = cmd.ResolveSwaggerUrl("/solution", "MyApp");

        Assert.Equal("https://localhost:7100/swagger", url);
    }

    [Fact]
    public void ResolveSwaggerUrl_NoLaunchSettings_ReturnsNull()
    {
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("launchSettings.json")))).Returns(false);

        var url = CreateCommand().ResolveSwaggerUrl("/solution", "MyApp");

        Assert.Null(url);
    }

    [Fact]
    public void ResolveSwaggerUrl_HttpOnly_ReturnsHttpUrl()
    {
        const string launchSettings = """
            {
              "profiles": {
                "MyApp": {
                  "applicationUrl": "http://localhost:5100"
                }
              }
            }
            """;
        _fileWriter.Setup(f => f.FileExists(It.Is<string>(p => p.EndsWith("launchSettings.json")))).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.Is<string>(p => p.EndsWith("launchSettings.json"))))
                   .Returns(launchSettings);

        var url = CreateCommand().ResolveSwaggerUrl("/solution", "MyApp");

        Assert.Equal("http://localhost:5100/swagger", url);
    }
}
