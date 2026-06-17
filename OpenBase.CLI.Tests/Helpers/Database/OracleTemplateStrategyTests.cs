using OpenBase.CLI.Helpers.Database;
using Xunit;

namespace OpenBase.CLI.Tests.Helpers.Database;

public class OracleTemplateStrategyTests
{
    [Fact]
    public void BuildConnectionString_ShouldIncludeDbName()
    {
        var strategy = new OracleTemplateStrategy();
        var dbName = "MYDB";
        var server = "localhost:1521";
        var user = "user";
        var password = "password";

        var connectionString = strategy.BuildConnectionString(dbName, server, user, password);

        // Expected format: Data Source=localhost:1521/MYDB;User Id=user;Password=password;
        Assert.Contains("Data Source=localhost:1521/MYDB", connectionString);
        Assert.Contains("User Id=user", connectionString);
        Assert.Contains("Password=password", connectionString);
    }

    [Fact]
    public void BuildConnectionString_ShouldUseServerIfAlreadyHasService()
    {
        var strategy = new OracleTemplateStrategy();
        var dbName = "IGNORED";
        var server = "localhost:1521/EXISTING";
        var user = "user";
        var password = "password";

        var connectionString = strategy.BuildConnectionString(dbName, server, user, password);

        // Expected format: Data Source=localhost:1521/EXISTING;User Id=user;Password=password;
        Assert.Contains("Data Source=localhost:1521/EXISTING", connectionString);
        Assert.Contains("User Id=user", connectionString);
        Assert.Contains("Password=password", connectionString);
    }
}
