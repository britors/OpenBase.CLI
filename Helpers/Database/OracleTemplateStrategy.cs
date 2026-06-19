using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public sealed class OracleTemplateStrategy : IDbTemplateStrategy
{
    private const string OracleShortName     = "openbasenet-oracle";
    private const string OracleConnectionKey = "OpenBaseOracle";
    private const string OracleDefaultServer = "localhost:1521";

    public string ShortName     => OracleShortName;
    public string ConnectionKey => OracleConnectionKey;
    public string DefaultServer => OracleDefaultServer;
    public DbFlavor DbFlavor    => DbFlavor.Oracle;

    public string BuildConnectionString(string dbName, string server, string user, string password)
    {
        var dataSource = server.Contains('/') ? server : $"{server}/{dbName}";
        return $"Data Source={dataSource};User Id={user};Password={password};";
    }
}
