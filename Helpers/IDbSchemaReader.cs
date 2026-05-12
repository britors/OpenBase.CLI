using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

public interface IDbSchemaReader
{
    IReadOnlyList<EntityProperty> ReadColumns(
        string connectionString,
        string schema,
        string tableName,
        DbFlavor dbFlavor);
}
