namespace OpenBase.CLI.Helpers;

public sealed class PostgresTemplateStrategy : IDbTemplateStrategy
{
    private const string PgShortName     = "openbasenet-pgsql";
    private const string PgConnectionKey = "OpenBasePostgres";
    private const string PgDefaultServer = "localhost";

    public string ShortName     => PgShortName;
    public string ConnectionKey => PgConnectionKey;
    public string DefaultServer => PgDefaultServer;

    public string BuildConnectionString(string projectName, string server, string user, string password)
        => string.IsNullOrWhiteSpace(user)
            ? $"Host={server};Database={projectName}"
            : $"Host={server};Database={projectName};Username={user};Password={password}";
}
