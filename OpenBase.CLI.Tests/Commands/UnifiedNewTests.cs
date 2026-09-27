using System.Text.Json;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public sealed class UnifiedNewTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "openbase-new-test-" + Guid.NewGuid());
    private readonly Mock<IDotNetRunner> runner = new();
    private readonly StringWriter output = new();
    private readonly IAnsiConsole console;
    public UnifiedNewTests()
    {
        console = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Interactive = InteractionSupport.No, Out = new AnsiConsoleOutput(output) });
        runner.Setup(r => r.IsSdkVersionSufficient(10)).Returns(true);
        runner.Setup(r => r.GetInstalledTemplateVersionAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync("11.0.0-preview.1");
        runner.Setup(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync((true, ""));
    }
    private NewSettings Settings(string? database = "postgres") => new() { Name = "Acme.Customers", Databases = database is null ? [] : [database], Output = root, Json = true };
    private Task<int> Run(NewSettings settings, CancellationToken token = default)
        => ((ICommand<NewSettings>)new NewCommand(runner.Object, console)).ExecuteAsync(CommandTestHelper.CreateContext(), settings, token);
    private string Error => JsonDocument.Parse(output.ToString()).RootElement.GetProperty("error").GetProperty("code").GetString()!;

    [Fact]
    public async Task MissingDatabaseNeverPromptsOrExecutes()
    {
        Assert.Equal(2, await Run(Settings(null)));
        Assert.Equal("INPUT_REQUIRED", Error);
        runner.Verify(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Never);
    }
    [Fact]
    public async Task MissingTemplateDoesNotInstallOrWrite()
    {
        runner.Setup(r => r.GetInstalledTemplateVersionAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        Assert.Equal(3, await Run(Settings()));
        Assert.Equal("TEMPLATE_NOT_INSTALLED", Error);
        Assert.False(Directory.Exists(root));
    }
    [Fact]
    public async Task OccupiedOutputIsPreserved()
    {
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "keep"), "original");
        Assert.Equal(2, await Run(Settings()));
        Assert.Equal("DESTINATION_NOT_EMPTY", Error);
        Assert.Equal("original", File.ReadAllText(Path.Combine(root, "keep")));
    }
    [Fact]
    public async Task UnsupportedInstalledMajorDoesNotGenerate()
    {
        runner.Setup(r => r.GetInstalledTemplateVersionAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync("12.0.0");
        Assert.Equal(3, await Run(Settings())); Assert.Equal("TEMPLATE_VERSION_UNSUPPORTED", Error);
    }
    [Fact]
    public async Task CancellationReturns130()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        Assert.Equal(130, await Run(Settings(), source.Token)); Assert.Equal("CANCELLED", Error);
    }
    [Fact]
    public async Task ChildCancellationPreservesPartialOutputAndStage()
    {
        runner.Setup(r => r.RunAsync(It.Is<IReadOnlyList<string>>(a => !a.Contains("--dry-run")), It.IsAny<CancellationToken>(), null))
            .Returns(() => { Directory.CreateDirectory(root); return Task.FromException<(bool, string)>(new OperationCanceledException()); });
        Assert.Equal(130, await Run(Settings())); Assert.True(Directory.Exists(root));
        Assert.Contains("generation", output.ToString());
    }
    [Theory]
    [InlineData("postgres")]
    [InlineData("sqlserver")]
    [InlineData("oracle")]
    public async Task GeneratesOnlyThroughUnifiedTemplateAndValidatesManifest(string database)
    {
        var settings = Settings(database); settings.Output = root + " path with spaces";
        Assert.Equal(2, await Run(settings)); // runner claims success but produces no manifest
        Assert.Equal("MANIFEST_INVALID", Error);
        runner.Verify(r => r.RunAsync(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "new", "openbasenet", "--name", "Acme.Customers", "--output", settings.Output, "--database", database })), It.IsAny<CancellationToken>(), null), Times.Once);
    }
    [Fact]
    public async Task ChildErrorsCannotLeakCredentials()
    {
        runner.Setup(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), null)).ReturnsAsync((false, "password-top-secret"));
        var settings = Settings(); settings.DbPassword = "password-top-secret";
        Assert.Equal(3, await Run(settings)); Assert.DoesNotContain("password-top-secret", output.ToString());
    }
    [Theory]
    [InlineData("postgres")]
    [InlineData("sqlserver")]
    [InlineData("oracle")]
    public void ConnectionBuilderEscapesValues(string database)
    {
        var s = Settings(database); s.DbPassword = "p;ass\"word"; s.DbUser = "test";
        var text = NewCommand.BuildConnectionString(database, s);
        System.Data.Common.DbConnectionStringBuilder b = database switch
        { "postgres" => new Npgsql.NpgsqlConnectionStringBuilder(text), "sqlserver" => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(text), _ => new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder(text) };
        Assert.Equal(s.DbPassword, b["Password"]);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); output.Dispose(); }
}
