namespace OpenBase.CLI.Helpers;

public sealed class PostgresTemplateStrategy : IDbTemplateStrategy
{
    public string ShortName => "openbasenet-pgsql";
    public string ConnectionKey => "OpenBasePostgres";
    public string DefaultServer => "localhost";

    public string BuildConnectionString(string projectName, string server, string user, string password)
        => string.IsNullOrEmpty(user)
            ? $"Host={server};Database={projectName}"
            : $"Host={server};Database={projectName};Username={user};Password={password}";
}
