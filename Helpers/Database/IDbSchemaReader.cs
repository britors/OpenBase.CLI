using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public interface IDbSchemaReader
{
    bool TryConnect(string connectionString, DbFlavor dbFlavor);

    IReadOnlyList<DbTableInfo> ListTables(string connectionString, DbFlavor dbFlavor);

    IReadOnlyList<EntityProperty> ReadColumns(
        string connectionString,
        string schema,
        string tableName,
        DbFlavor dbFlavor);

    IReadOnlyList<DbProcedureInfo> ListProcedures(string connectionString, DbFlavor dbFlavor);

    IReadOnlyList<ProcedureParameter> ReadProcedureParameters(
        string connectionString,
        string schema,
        string procedureName,
        DbFlavor dbFlavor);
}
