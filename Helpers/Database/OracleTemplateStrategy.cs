namespace OpenBase.CLI.Helpers.Database;

public sealed class OracleTemplateStrategy : IDbTemplateStrategy
{
    private const string OracleShortName     = "openbasenet-oracle";
    private const string OracleConnectionKey = "OpenBaseOracle";
    private const string OracleDefaultServer = "localhost:1521/XEPDB1";

    public string ShortName     => OracleShortName;
    public string ConnectionKey => OracleConnectionKey;
    public string DefaultServer => OracleDefaultServer;

    public string BuildConnectionString(string dbName, string server, string user, string password)
        => $"Data Source={server};User Id={user};Password={password};";
}
