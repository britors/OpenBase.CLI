using OpenBase.CLI.Helpers.Database;
namespace OpenBase.CLI.Tests.Commands;
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
