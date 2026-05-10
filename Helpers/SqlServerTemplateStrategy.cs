namespace OpenBase.CLI.Helpers;

public sealed class SqlServerTemplateStrategy : IDbTemplateStrategy
{
    public string ShortName => "openbasenet-sql";
    public string ConnectionKey => "OpenBaseSQLServer";
    public string DefaultServer => ".";

    public string BuildConnectionString(string projectName, string server, string user, string password)
        => string.IsNullOrEmpty(user)
            ? $"Server={server};Database={projectName};Trusted_Connection=True;TrustServerCertificate=True"
            : $"Server={server};Database={projectName};User Id={user};Password={password};TrustServerCertificate=True";
}
