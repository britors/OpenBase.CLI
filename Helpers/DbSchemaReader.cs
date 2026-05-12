using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Npgsql;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class DbSchemaReader : IDbSchemaReader
{
    private const string ColumnQuery = """
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns
        WHERE table_schema = @schema AND table_name = @tableName
        ORDER BY ordinal_position
        """;

    private const string PkQuery = """
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
           AND tc.table_schema    = kcu.table_schema
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_schema = @schema
          AND tc.table_name   = @tableName
        """;

    public IReadOnlyList<EntityProperty> ReadColumns(
        string connectionString,
        string schema,
        string tableName,
        DbFlavor dbFlavor)
    {
        using DbConnection conn = dbFlavor == DbFlavor.Postgres
            ? new NpgsqlConnection(connectionString)
            : new SqlConnection(connectionString);

        conn.Open();

        var pkColumns = ReadPrimaryKeys(conn, schema, tableName);
        var properties = new List<EntityProperty>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = ColumnQuery;
        AddParam(cmd, "@schema", schema);
        AddParam(cmd, "@tableName", tableName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var colName  = reader.GetString(0);
            var dataType = reader.GetString(1);
            var nullable = reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase);

            if (pkColumns.Contains(colName))
                continue;

            var csType   = SqlTypeMapper.ToCSharpType(dataType, dbFlavor);
            var propName = SqlTypeMapper.ToPascalCase(colName);

            properties.Add(new EntityProperty(propName, csType, !nullable));
        }

        return properties;
    }

    private static HashSet<string> ReadPrimaryKeys(DbConnection conn, string schema, string tableName)
    {
        var pks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = PkQuery;
        AddParam(cmd, "@schema", schema);
        AddParam(cmd, "@tableName", tableName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            pks.Add(reader.GetString(0));

        return pks;
    }

    private static void AddParam(DbCommand cmd, string name, string value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
