using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class VersionCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    private VersionCommand CreateCommand() => new(_dotNetRunner.Object);

    [Fact]
    public async Task Execute_ReturnsZero()
    {
        _dotNetRunner.Setup(r => r.GetDotnetVersion()).Returns("10.0.0");

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("version"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Execute_CallsGetDotnetVersion()
    {
        _dotNetRunner.Setup(r => r.GetDotnetVersion()).Returns("10.0.1");

        await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("version"), new VersionSettings(), CancellationToken.None);

        _dotNetRunner.Verify(r => r.GetDotnetVersion(), Times.Once);
    }

    [Fact]
    public async Task Execute_UnknownVersion_ReturnsZero()
    {
        _dotNetRunner.Setup(r => r.GetDotnetVersion()).Returns("--");

        var result = await ((ICommand<VersionSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("version"), new VersionSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }
}
