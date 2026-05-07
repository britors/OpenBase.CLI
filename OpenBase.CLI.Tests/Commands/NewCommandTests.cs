using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class NewCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    public NewCommandTests()
    {
        _dotNetRunner
            .Setup(r => r.IsSdkVersionSufficient(It.IsAny<int>()))
            .Returns(true);
    }

    private NewCommand CreateCommand() =>
        new(_dotNetRunner.Object, CommandTestHelper.CreateConsole());

    private static NewSettings BuildSettings(string type = "api", string template = "sqlserver", string name = "MeuProjeto") =>
        new() { Type = type, TemplateName = template, Name = name };

    private void SetupRun(bool success, string error = "") =>
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((success, error));

    private void SetupSdkVersion(bool sufficient) =>
        _dotNetRunner
            .Setup(r => r.IsSdkVersionSufficient(It.IsAny<int>()))
            .Returns(sufficient);

    [Fact]
    public async Task ExecuteAsync_SdkVersionInsufficient_ReturnsOne()
    {
        SetupSdkVersion(false);
        var settings = BuildSettings(name: "MinhaApi");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTemplateKey_ReturnsOne()
    {
        var settings = BuildSettings(type: "web", template: "unknown");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ValidSqlServerKey_DotnetSucceeds_ReturnsZero()
    {
        SetupRun(true);
        var settings = BuildSettings(type: "api", template: "sqlserver", name: "MinhaApi");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPgsqlKey_DotnetSucceeds_ReturnsZero()
    {
        SetupRun(true);
        var settings = BuildSettings(type: "api", template: "pgsql", name: "MinhaApi");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_WithoutErrorMessage_ReturnsOne()
    {
        SetupRun(false, string.Empty);
        var settings = BuildSettings(name: "MinhaApi");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_WithErrorMessage_ReturnsOne()
    {
        SetupRun(false, "Template não encontrado");
        var settings = BuildSettings(name: "MinhaApi");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectDotnetArguments()
    {
        SetupRun(true);
        var settings = BuildSettings(type: "api", template: "sqlserver", name: "MinhaApi");

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

        _dotNetRunner.Verify(
            r => r.RunAsync("new openbasenet-sql -n MinhaApi -o MinhaApi", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
