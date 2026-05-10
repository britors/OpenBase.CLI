namespace OpenBase.CLI.Helpers;

public interface IDbTemplateStrategy
{
    string ShortName { get; }
    string ConnectionKey { get; }
    string DefaultServer { get; }
    string BuildConnectionString(string projectName, string server, string user, string password);
}
