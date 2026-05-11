using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class VersionRestoreCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();

    private VersionRestoreCommand CreateCommand() =>
        new(_dotNetRunner.Object, CommandTestHelper.CreateConsole());

    private void SetupRun(bool success, string error = "") =>
        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((success, error));

    private static VersionRestoreSettings MakeSettings(string version, string? type) =>
        new() { Version = version, Type = type };

    // --- Validate() ---

    [Theory]
    [InlineData("cli")]
    [InlineData("CLI")]
    [InlineData("sqlserver")]
    [InlineData("SQLServer")]
    [InlineData("postgres")]
    [InlineData("POSTGRES")]
    public void Validate_ValidType_ReturnsSuccess(string type)
    {
        var result = MakeSettings("1.0.0", type).Validate();
        Assert.True(result.Successful);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingType_ReturnsError(string? type)
    {
        var result = MakeSettings("1.0.0", type).Validate();
        Assert.False(result.Successful);
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("oracle")]
    [InlineData("unknown")]
    public void Validate_InvalidType_ReturnsError(string type)
    {
        var result = MakeSettings("1.0.0", type).Validate();
        Assert.False(result.Successful);
    }

    // --- ExecuteAsync ---

    [Fact]
    public async Task ExecuteAsync_CliSucceeds_ReturnsZero()
    {
        SetupRun(true);

        var result = await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("10.5.9", "cli"), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CliFails_ReturnsOne()
    {
        SetupRun(false, "versão não encontrada no NuGet");

        var result = await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("10.5.9", "cli"), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    public async Task ExecuteAsync_TemplateSucceeds_ReturnsZero(string type)
    {
        SetupRun(true);

        var result = await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("2.0.0", type), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CliRestore_UsesToolUpdateCommand()
    {
        SetupRun(true);

        await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("10.5.9", "cli"), CancellationToken.None);

        _dotNetRunner.Verify(r =>
            r.RunAsync("tool update -g w3ti.OpenBase.CLI --version 10.5.9", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SqlServerRestore_UsesNewInstallWithVersion()
    {
        SetupRun(true);

        await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("2.0.0", "sqlserver"), CancellationToken.None);

        _dotNetRunner.Verify(r =>
            r.RunAsync("new install w3ti.OpenBaseNET.SQLServer.Template::2.0.0", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PostgresRestore_UsesNewInstallWithVersion()
    {
        SetupRun(true);

        await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("1.5.3", "postgres"), CancellationToken.None);

        _dotNetRunner.Verify(r =>
            r.RunAsync("new install w3ti.OpenBaseNET.Postgres.Template::1.5.3", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CliFails_WithErrorMessage_ReturnsOne()
    {
        SetupRun(false, "pacote não encontrado");

        var result = await ((ICommand<VersionRestoreSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("restore"), MakeSettings("0.0.1", "cli"), CancellationToken.None);

        Assert.Equal(1, result);
    }
}
