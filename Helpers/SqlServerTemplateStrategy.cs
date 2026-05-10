namespace OpenBase.CLI.Helpers;

public sealed class SqlServerTemplateStrategy : IDbTemplateStrategy
{
    private const string SqlShortName     = "openbasenet-sql";
    private const string SqlConnectionKey = "OpenBaseSQLServer";
    private const string SqlDefaultServer = ".";

    public string ShortName     => SqlShortName;
    public string ConnectionKey => SqlConnectionKey;
    public string DefaultServer => SqlDefaultServer;

    public string BuildConnectionString(string projectName, string server, string user, string password)
        => string.IsNullOrWhiteSpace(user)
            ? $"Server={server};Database={projectName};Trusted_Connection=True;TrustServerCertificate=True"
            : $"Server={server};Database={projectName};User Id={user};Password={password};TrustServerCertificate=True";
}
