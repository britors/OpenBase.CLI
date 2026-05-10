using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class NewCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IProjectConfigurator> _configurator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    private static readonly ProjectSetupConfig DefaultConfig = new("", "", ".", "", "");

    public NewCommandTests()
    {
        _dotNetRunner
            .Setup(r => r.IsSdkVersionSufficient(It.IsAny<int>()))
            .Returns(true);

        _configurator
            .Setup(c => c.Collect(It.IsAny<IDbTemplateStrategy>()))
            .Returns(DefaultConfig);

        _fileWriter
            .Setup(f => f.FileExists(It.IsAny<string>()))
            .Returns(false);
    }

    private NewCommand CreateCommand() =>
        new(_dotNetRunner.Object, CommandTestHelper.CreateConsole(), _configurator.Object, _fileWriter.Object);

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

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTemplateKey_ReturnsOne()
    {
        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "web", template: "unknown"), CancellationToken.None);

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ValidSqlServerKey_DotnetSucceeds_ReturnsZero()
    {
        SetupRun(true);

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "api", template: "sqlserver", name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidPgsqlKey_DotnetSucceeds_ReturnsZero()
    {
        SetupRun(true);

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "api", template: "pgsql", name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_WithoutErrorMessage_ReturnsOne()
    {
        SetupRun(false, string.Empty);

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_WithErrorMessage_ReturnsOne()
    {
        SetupRun(false, "Template não encontrado");

        var result = await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_SqlServer_PassesCorrectDotnetArguments()
    {
        SetupRun(true);

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "api", template: "sqlserver", name: "MinhaApi"), CancellationToken.None);

        _dotNetRunner.Verify(
            r => r.RunAsync("new openbasenet-sql -n MinhaApi -o MinhaApi", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Pgsql_PassesCorrectDotnetArguments()
    {
        SetupRun(true);

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "api", template: "pgsql", name: "MinhaApi"), CancellationToken.None);

        _dotNetRunner.Verify(
            r => r.RunAsync("new openbasenet-pgsql -n MinhaApi -o MinhaApi", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PassesStrategyToConfigurator()
    {
        SetupRun(true);

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(template: "pgsql", name: "MinhaApi"), CancellationToken.None);

        _configurator.Verify(
            c => c.Collect(It.Is<IDbTemplateStrategy>(s => s is PostgresTemplateStrategy)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetSucceeds_CollectsConfigBeforeRun()
    {
        SetupRun(true);
        var callOrder = new List<string>();

        _configurator
            .Setup(c => c.Collect(It.IsAny<IDbTemplateStrategy>()))
            .Callback<IDbTemplateStrategy>(_ => callOrder.Add("collect"))
            .Returns(DefaultConfig);

        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => callOrder.Add("run"))
            .ReturnsAsync((true, string.Empty));

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        Assert.Equal(["collect", "run"], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetSucceeds_UpdatesAppSettings()
    {
        SetupRun(true);
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>()))
            .Returns("""{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""");

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_DoesNotUpdateAppSettings()
    {
        SetupRun(false);

        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(name: "MinhaApi"), CancellationToken.None);

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTemplate_DoesNotCollectConfig()
    {
        await ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), BuildSettings(type: "web", template: "unknown"), CancellationToken.None);

        _configurator.Verify(c => c.Collect(It.IsAny<IDbTemplateStrategy>()), Times.Never);
    }
}

// ── Strategy: SQL Server ───────────────────────────────────────────────────────

public class SqlServerTemplateStrategyTests
{
    private readonly SqlServerTemplateStrategy _strategy = new();

    [Fact]
    public void ShortName_IsCorrect() => Assert.Equal("openbasenet-sql", _strategy.ShortName);

    [Fact]
    public void ConnectionKey_IsCorrect() => Assert.Equal("OpenBaseSQLServer", _strategy.ConnectionKey);

    [Fact]
    public void DefaultServer_IsDot() => Assert.Equal(".", _strategy.DefaultServer);

    [Fact]
    public void WithCredentials_UsesUserIdPassword()
    {
        var cs = _strategy.BuildConnectionString("MeuProjeto", "myserver", "sa", "secret");

        Assert.Contains("Server=myserver", cs);
        Assert.Contains("Database=MeuProjeto", cs);
        Assert.Contains("User Id=sa", cs);
        Assert.Contains("Password=secret", cs);
        Assert.Contains("TrustServerCertificate=True", cs);
        Assert.DoesNotContain("Trusted_Connection", cs);
    }

    [Fact]
    public void WithoutUser_UsesTrustedConnection()
    {
        var cs = _strategy.BuildConnectionString("MeuProjeto", ".", "", "");

        Assert.Contains("Trusted_Connection=True", cs);
        Assert.DoesNotContain("User Id", cs);
        Assert.DoesNotContain("Password", cs);
    }
}

// ── Strategy: PostgreSQL ──────────────────────────────────────────────────────

public class PostgresTemplateStrategyTests
{
    private readonly PostgresTemplateStrategy _strategy = new();

    [Fact]
    public void ShortName_IsCorrect() => Assert.Equal("openbasenet-pgsql", _strategy.ShortName);

    [Fact]
    public void ConnectionKey_IsCorrect() => Assert.Equal("OpenBasePostgres", _strategy.ConnectionKey);

    [Fact]
    public void DefaultServer_IsLocalhost() => Assert.Equal("localhost", _strategy.DefaultServer);

    [Fact]
    public void WithCredentials_UsesUsernamePassword()
    {
        var cs = _strategy.BuildConnectionString("MeuProjeto", "localhost", "postgres", "secret");

        Assert.Contains("Host=localhost", cs);
        Assert.Contains("Database=MeuProjeto", cs);
        Assert.Contains("Username=postgres", cs);
        Assert.Contains("Password=secret", cs);
    }

    [Fact]
    public void WithoutUser_OmitsCredentials()
    {
        var cs = _strategy.BuildConnectionString("MeuProjeto", "localhost", "", "");

        Assert.Contains("Host=localhost", cs);
        Assert.DoesNotContain("Username", cs);
        Assert.DoesNotContain("Password", cs);
    }
}

// ── ApplyConfigToJson ─────────────────────────────────────────────────────────

public class NewCommandApplyConfigToJsonTests
{
    private static ProjectSetupConfig Config(string mediatr = "", string automapper = "") =>
        new(mediatr, automapper, ".", "sa", "secret");

    [Fact]
    public void UpdatesSqlServerConnectionString()
    {
        var json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, "OpenBaseSQLServer", "Server=.;Database=Test", Config());

        Assert.Contains("Server=.;Database=Test", result);
    }

    [Fact]
    public void UpdatesMediatorLicenseKey()
    {
        var json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, "OpenBaseSQLServer", "cs", Config(mediatr: "mtr-key-123"));

        Assert.Contains("mtr-key-123", result);
    }

    [Fact]
    public void UpdatesMediatrLicenseKey()
    {
        var json = """{"ConnectionStrings":{"OpenBasePostgres":""},"Mediatr":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, "OpenBasePostgres", "cs", Config(mediatr: "mtr-key-123"));

        Assert.Contains("mtr-key-123", result);
    }

    [Fact]
    public void UpdatesAutoMapperLicenseKey()
    {
        var json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"AutoMapper":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, "OpenBaseSQLServer", "cs", Config(automapper: "am-key-456"));

        Assert.Contains("am-key-456", result);
    }

    [Fact]
    public void UpdatesAutomapperLicenseKey()
    {
        var json = """{"ConnectionStrings":{"OpenBasePostgres":""},"Automapper":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, "OpenBasePostgres", "cs", Config(automapper: "am-key-456"));

        Assert.Contains("am-key-456", result);
    }

    [Fact]
    public void InvalidJson_ReturnsOriginalContent()
    {
        const string invalid = "not json";

        var result = NewCommand.ApplyConfigToJson(invalid, "key", "cs", Config());

        Assert.Equal(invalid, result);
    }
}

// ── UpdateAppSettings ─────────────────────────────────────────────────────────

public class NewCommandUpdateAppSettingsTests
{
    private readonly Mock<IFileWriter> _fileWriter = new();
    private readonly SqlServerTemplateStrategy _sqlStrategy = new();
    private readonly PostgresTemplateStrategy _pgStrategy = new();

    private void SetupFile(string path, string content)
    {
        _fileWriter.Setup(f => f.FileExists(path)).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(path)).Returns(content);
    }

    [Fact]
    public void UpdatesBothAppsettingsFiles()
    {
        const string name = "MinhaApi";
        var basePath = Path.Combine(name, "src", $"{name}.Presentation.Api");
        var prod = Path.Combine(basePath, "appsettings.json");
        var dev = Path.Combine(basePath, "appsettings.Development.json");
        const string json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""";

        SetupFile(prod, json);
        SetupFile(dev, json);

        NewCommand.UpdateAppSettings(name, _sqlStrategy, new ProjectSetupConfig("", "", ".", "sa", "pwd"), _fileWriter.Object);

        _fileWriter.Verify(f => f.WriteAllText(prod, It.IsAny<string>()), Times.Once);
        _fileWriter.Verify(f => f.WriteAllText(dev, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SkipsFileThatDoesNotExist()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        NewCommand.UpdateAppSettings("Proj", _sqlStrategy, new ProjectSetupConfig("", "", ".", "", ""), _fileWriter.Object);

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SqlServer_WritesCorrectConnectionKey()
    {
        const string name = "MinhaApi";
        var path = Path.Combine(name, "src", $"{name}.Presentation.Api", "appsettings.json");
        SetupFile(path, """{"ConnectionStrings":{"OpenBaseSQLServer":""}}""");

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(path, It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        NewCommand.UpdateAppSettings(name, _sqlStrategy, new ProjectSetupConfig("", "", ".", "sa", "pwd"), _fileWriter.Object);

        Assert.NotNull(written);
        Assert.Contains("OpenBaseSQLServer", written);
    }

    [Fact]
    public void Pgsql_WritesCorrectConnectionKey()
    {
        const string name = "MinhaApi";
        var path = Path.Combine(name, "src", $"{name}.Presentation.Api", "appsettings.json");
        SetupFile(path, """{"ConnectionStrings":{"OpenBasePostgres":""}}""");

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(path, It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        NewCommand.UpdateAppSettings(name, _pgStrategy, new ProjectSetupConfig("", "", "localhost", "pg", "pwd"), _fileWriter.Object);

        Assert.NotNull(written);
        Assert.Contains("OpenBasePostgres", written);
    }
}
