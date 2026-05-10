using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class NewCommandTests
{
    private readonly Mock<IDotNetRunner> _dotNetRunner = new();
    private readonly Mock<IProjectConfigurator> _configurator = new();
    private readonly Mock<IFileWriter> _fileWriter = new();

    private const string ProjectName = "MinhaApi";

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

    private Task<int> RunAsync(NewSettings settings) =>
        ((ICommand<NewSettings>)CreateCommand())
            .ExecuteAsync(CommandTestHelper.CreateContext("new"), settings, CancellationToken.None);

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

        var result = await RunAsync(BuildSettings(name: ProjectName));

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTemplateKey_ReturnsOne()
    {
        var result = await RunAsync(BuildSettings(type: "web", template: "unknown"));

        Assert.Equal(1, result);
        _dotNetRunner.Verify(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("pgsql")]
    public async Task ExecuteAsync_ValidTemplate_DotnetSucceeds_ReturnsZero(string template)
    {
        SetupRun(true);

        var result = await RunAsync(BuildSettings(template: template, name: ProjectName));

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Template não encontrado")]
    public async Task ExecuteAsync_DotnetFails_ReturnsOne(string error)
    {
        SetupRun(false, error);

        var result = await RunAsync(BuildSettings(name: ProjectName));

        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("sqlserver", "new openbasenet-sql -n "  + ProjectName + " -o " + ProjectName)]
    [InlineData("pgsql",     "new openbasenet-pgsql -n " + ProjectName + " -o " + ProjectName)]
    public async Task ExecuteAsync_PassesCorrectDotnetArguments(string template, string expectedArgs)
    {
        SetupRun(true);

        await RunAsync(BuildSettings(template: template, name: ProjectName));

        _dotNetRunner.Verify(r => r.RunAsync(expectedArgs, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PassesStrategyToConfigurator()
    {
        SetupRun(true);

        await RunAsync(BuildSettings(template: "pgsql", name: ProjectName));

        _configurator.Verify(
            c => c.Collect(It.Is<IDbTemplateStrategy>(s => s is PostgresTemplateStrategy)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetSucceeds_CollectsConfigBeforeRun()
    {
        var callOrder = new List<string>();

        _configurator
            .Setup(c => c.Collect(It.IsAny<IDbTemplateStrategy>()))
            .Callback<IDbTemplateStrategy>(_ => callOrder.Add("collect"))
            .Returns(DefaultConfig);

        _dotNetRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => callOrder.Add("run"))
            .ReturnsAsync((true, string.Empty));

        await RunAsync(BuildSettings(name: ProjectName));

        Assert.Equal(["collect", "run"], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetSucceeds_UpdatesAppSettings()
    {
        SetupRun(true);
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(It.IsAny<string>()))
            .Returns("""{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""");

        await RunAsync(BuildSettings(name: ProjectName));

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_DotnetFails_DoesNotUpdateAppSettings()
    {
        SetupRun(false);

        await RunAsync(BuildSettings(name: ProjectName));

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTemplate_DoesNotCollectConfig()
    {
        await RunAsync(BuildSettings(type: "web", template: "unknown"));

        _configurator.Verify(c => c.Collect(It.IsAny<IDbTemplateStrategy>()), Times.Never);
    }
}

// ── Strategy: SQL Server ──────────────────────────────────────────────────────

public class SqlServerTemplateStrategyTests
{
    private const string SampleProject = "MeuProjeto";

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
        var cs = _strategy.BuildConnectionString(SampleProject, "myserver", "sa", "secret");

        Assert.Contains("Server=myserver", cs);
        Assert.Contains($"Database={SampleProject}", cs);
        Assert.Contains("User Id=sa", cs);
        Assert.Contains("Password=secret", cs);
        Assert.Contains("TrustServerCertificate=True", cs);
        Assert.DoesNotContain("Trusted_Connection", cs);
    }

    [Fact]
    public void WithoutUser_UsesTrustedConnection()
    {
        var cs = _strategy.BuildConnectionString(SampleProject, ".", "", "");

        Assert.Contains("Trusted_Connection=True", cs);
        Assert.DoesNotContain("User Id", cs);
        Assert.DoesNotContain("Password", cs);
    }
}

// ── Strategy: PostgreSQL ──────────────────────────────────────────────────────

public class PostgresTemplateStrategyTests
{
    private const string SampleProject = "MeuProjeto";

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
        var cs = _strategy.BuildConnectionString(SampleProject, "localhost", "postgres", "secret");

        Assert.Contains("Host=localhost", cs);
        Assert.Contains($"Database={SampleProject}", cs);
        Assert.Contains("Username=postgres", cs);
        Assert.Contains("Password=secret", cs);
    }

    [Fact]
    public void WithoutUser_OmitsCredentials()
    {
        var cs = _strategy.BuildConnectionString(SampleProject, "localhost", "", "");

        Assert.Contains("Host=localhost", cs);
        Assert.DoesNotContain("Username", cs);
        Assert.DoesNotContain("Password", cs);
    }
}

// ── ApplyConfigToJson ─────────────────────────────────────────────────────────

public class NewCommandApplyConfigToJsonTests
{
    private const string SqlServerKey = "OpenBaseSQLServer";
    private const string PostgresKey  = "OpenBasePostgres";

    private static ProjectSetupConfig Config(string mediatr = "", string automapper = "") =>
        new(mediatr, automapper, ".", "sa", "secret");

    [Fact]
    public void UpdatesConnectionString()
    {
        const string json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""";

        var result = NewCommand.ApplyConfigToJson(json, SqlServerKey, "Server=.;Database=Test", Config());

        Assert.Contains("Server=.;Database=Test", result);
    }

    [Theory]
    [InlineData("""{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""",  SqlServerKey)]
    [InlineData("""{"ConnectionStrings":{"OpenBasePostgres":""},"Mediatr":{"LicenseKey":""}}""",    PostgresKey)]
    public void UpdatesMediatRLicenseKey(string json, string connKey)
    {
        var result = NewCommand.ApplyConfigToJson(json, connKey, "cs", Config(mediatr: "mtr-key-123"));

        Assert.Contains("mtr-key-123", result);
    }

    [Theory]
    [InlineData("""{"ConnectionStrings":{"OpenBaseSQLServer":""},"AutoMapper":{"LicenseKey":""}}""", SqlServerKey)]
    [InlineData("""{"ConnectionStrings":{"OpenBasePostgres":""},"Automapper":{"LicenseKey":""}}""",  PostgresKey)]
    public void UpdatesAutoMapperLicenseKey(string json, string connKey)
    {
        var result = NewCommand.ApplyConfigToJson(json, connKey, "cs", Config(automapper: "am-key-456"));

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
    private const string ProjectName  = "MinhaApi";
    private const string ApiSourceDir = "src";
    private const string ApiSuffix    = ".Presentation.Api";

    private readonly Mock<IFileWriter> _fileWriter = new();

    private void SetupFile(string path, string content)
    {
        _fileWriter.Setup(f => f.FileExists(path)).Returns(true);
        _fileWriter.Setup(f => f.ReadAllText(path)).Returns(content);
    }

    [Fact]
    public void UpdatesBothAppsettingsFiles()
    {
        var basePath = Path.Combine(ProjectName, ApiSourceDir, $"{ProjectName}{ApiSuffix}");
        var prod = Path.Combine(basePath, "appsettings.json");
        var dev  = Path.Combine(basePath, "appsettings.Development.json");
        const string json = """{"ConnectionStrings":{"OpenBaseSQLServer":""},"Mediator":{"LicenseKey":""}}""";

        SetupFile(prod, json);
        SetupFile(dev, json);

        NewCommand.UpdateAppSettings(ProjectName, new SqlServerTemplateStrategy(), new ProjectSetupConfig("", "", ".", "sa", "pwd"), _fileWriter.Object);

        _fileWriter.Verify(f => f.WriteAllText(prod, It.IsAny<string>()), Times.Once);
        _fileWriter.Verify(f => f.WriteAllText(dev,  It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void SkipsFileThatDoesNotExist()
    {
        _fileWriter.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        NewCommand.UpdateAppSettings(ProjectName, new SqlServerTemplateStrategy(), new ProjectSetupConfig("", "", ".", "", ""), _fileWriter.Object);

        _fileWriter.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    public static TheoryData<IDbTemplateStrategy, string, string, ProjectSetupConfig> ConnectionKeyData => new()
    {
        { new SqlServerTemplateStrategy(), """{"ConnectionStrings":{"OpenBaseSQLServer":""}}""", "OpenBaseSQLServer", new ProjectSetupConfig("", "", ".", "sa", "pwd") },
        { new PostgresTemplateStrategy(),  """{"ConnectionStrings":{"OpenBasePostgres":""}}""",  "OpenBasePostgres",  new ProjectSetupConfig("", "", "localhost", "pg", "pwd") },
    };

    [Theory]
    [MemberData(nameof(ConnectionKeyData))]
    public void WritesCorrectConnectionKey(IDbTemplateStrategy strategy, string json, string expectedKey, ProjectSetupConfig config)
    {
        var path = Path.Combine(ProjectName, ApiSourceDir, $"{ProjectName}{ApiSuffix}", "appsettings.json");
        SetupFile(path, json);

        string? written = null;
        _fileWriter.Setup(f => f.WriteAllText(path, It.IsAny<string>()))
            .Callback<string, string>((_, c) => written = c);

        NewCommand.UpdateAppSettings(ProjectName, strategy, config, _fileWriter.Object);

        Assert.NotNull(written);
        Assert.Contains(expectedKey, written);
    }
}
