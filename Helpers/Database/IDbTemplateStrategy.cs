using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public interface IDbTemplateStrategy
{
    string ShortName { get; }
    string ConnectionKey { get; }
    string DefaultServer { get; }
    DbFlavor DbFlavor { get; }
    string BuildConnectionString(string dbName, string server, string user, string password);
}
