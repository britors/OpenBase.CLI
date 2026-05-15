using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class BuildCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IProjectLocator> _projectLocator = new();
    private readonly Mock<ICsprojLocator> _csprojLocator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    public BuildCommandTests()
    {
        SR.Configure();
        _dotNetRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, string.Empty));
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), null))
                       .Returns(("/solution", "MyApp"));
        _fileWriter.Setup(f => f.FindSolutionFile("/solution"))
                   .Returns("/solution/MyApp.sln");
    }

    private BuildCommand CreateCommand() =>
        new(CommandTestHelper.CreateConsole(), _dotNetRunner.Object,
            _projectLocator.Object, _csprojLocator.Object, _fileWriter.Object);

    private static Task<int> Execute(BuildCommand cmd, BuildSettings settings) =>
        ((ICommand<BuildSettings>)cmd).ExecuteAsync(
            CommandTestHelper.CreateContext("build"), settings, CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ReturnsZero()
    {
        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_NoProjectFound_ReturnsOne()
    {
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), null)).Returns((null, null));
        _csprojLocator.Setup(c => c.Find(It.IsAny<string>())).Returns((string?)null);

        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToCsproj_WhenNoSln()
    {
        _projectLocator.Setup(p => p.Detect(It.IsAny<string>(), null)).Returns((null, null));
        _csprojLocator.Setup(c => c.Find(It.IsAny<string>())).Returns("/project/MyApp.csproj");

        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(0, result);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.Contains("MyApp.csproj")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultSettings_RunsRestoreBuildTest()
    {
        await Execute(CreateCommand(), new BuildSettings());

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("restore")), It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build")), It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoRestore_SkipsRestore()
    {
        await Execute(CreateCommand(), new BuildSettings { NoRestore = true });

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("restore")), It.IsAny<CancellationToken>()), Times.Never);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build")), It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RestoreFails_SkipsBuildAndTest()
    {
        _dotNetRunner.Setup(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("restore")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "restore error"));

        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build")), It.IsAny<CancellationToken>()), Times.Never);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test")), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_BuildFails_SkipsTest()
    {
        _dotNetRunner.Setup(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "build error"));

        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test")), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_TestFails_ReturnsOne()
    {
        _dotNetRunner.Setup(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test")), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "test failed"));

        var result = await Execute(CreateCommand(), new BuildSettings());

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfiguration_InBuildAndTestArgs()
    {
        await Execute(CreateCommand(), new BuildSettings { Configuration = "Release" });

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build") && s.Contains("Release")),
            It.IsAny<CancellationToken>()), Times.Once);
        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test") && s.Contains("Release")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BuildArgs_ContainNoRestore()
    {
        await Execute(CreateCommand(), new BuildSettings());

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("build") && s.Contains("--no-restore")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_TestArgs_ContainNoBuild()
    {
        await Execute(CreateCommand(), new BuildSettings());

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.StartsWith("test") && s.Contains("--no-build")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesSolutionFile_WhenFound()
    {
        await Execute(CreateCommand(), new BuildSettings());

        _dotNetRunner.Verify(r => r.RunAsync(
            It.Is<string>(s => s.Contains("MyApp.sln")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
