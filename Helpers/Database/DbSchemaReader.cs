using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public sealed class DbSchemaReader : IDbSchemaReader
{
    private const string AnsiListTablesQuery = """
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_type = 'BASE TABLE'
          AND table_schema NOT IN ('sys', 'INFORMATION_SCHEMA', 'guest', 'pg_catalog', 'information_schema')
        ORDER BY table_schema, table_name
        """;

    private const string OracleListTablesQuery = """
        SELECT owner, table_name
        FROM all_tables
        WHERE owner NOT IN (
            'SYS', 'SYSTEM', 'OUTLN', 'DBSNMP', 'ORACLE_OCM', 'MDSYS', 'ORDSYS',
            'ORDDATA', 'XDB', 'WMSYS', 'CTXSYS', 'EXFSYS', 'DVSYS', 'LBACSYS',
            'OJVMSYS', 'OLAPSYS', 'APPQOSSYS', 'AUDSYS', 'GSMADMIN_INTERNAL',
            'XS$NULL', 'GGSYS', 'GSMCATUSER', 'GSMUSER', 'SYSRAC', 'DVF',
            'SYSBACKUP', 'SYSDG', 'SYSKM', 'DBSFWUSER', 'REMOTE_SCHEDULER_AGENT'
        )
        ORDER BY owner, table_name
        """;

    public bool TryConnect(string connectionString, DbFlavor dbFlavor)
    {
        try
        {
            using DbConnection conn = dbFlavor switch
            {
                DbFlavor.Postgres => new NpgsqlConnection(connectionString),
                DbFlavor.Oracle   => new OracleConnection(connectionString),
                _                 => new SqlConnection(connectionString),
            };
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<DbTableInfo> ListTables(string connectionString, DbFlavor dbFlavor)
    {
        using DbConnection conn = dbFlavor switch
        {
            DbFlavor.Postgres => new NpgsqlConnection(connectionString),
            DbFlavor.Oracle   => new OracleConnection(connectionString),
            _                 => new SqlConnection(connectionString),
        };
        conn.Open();

        var tables = new List<DbTableInfo>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = dbFlavor == DbFlavor.Oracle ? OracleListTablesQuery : AnsiListTablesQuery;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            tables.Add(new DbTableInfo(reader.GetString(0), reader.GetString(1)));

        return tables;
    }


    private const string AnsiColumnQuery = """
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns
        WHERE table_schema = @schema AND table_name = @tableName
        ORDER BY ordinal_position
        """;

    private const string AnsiPkQuery = """
        SELECT kcu.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
           AND tc.table_schema    = kcu.table_schema
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_schema = @schema
          AND tc.table_name   = @tableName
        """;

    private const string OracleColumnQuery = """
        SELECT column_name, data_type, nullable
        FROM all_tab_columns
        WHERE owner = UPPER(:schema) AND table_name = UPPER(:tableName)
        ORDER BY column_id
        """;

    private const string OraclePkQuery = """
        SELECT acc.column_name
        FROM all_constraints ac
        JOIN all_cons_columns acc
          ON ac.constraint_name = acc.constraint_name AND ac.owner = acc.owner
        WHERE ac.constraint_type = 'P'
          AND ac.owner = UPPER(:schema)
          AND ac.table_name = UPPER(:tableName)
        """;

    public IReadOnlyList<EntityProperty> ReadColumns(
        string connectionString,
        string schema,
        string tableName,
        DbFlavor dbFlavor)
    {
        using DbConnection conn = dbFlavor switch
        {
            DbFlavor.Postgres => new NpgsqlConnection(connectionString),
            DbFlavor.Oracle   => new OracleConnection(connectionString),
            _                 => new SqlConnection(connectionString),
        };

        conn.Open();

        var pkColumns  = ReadPrimaryKeys(conn, schema, tableName, dbFlavor);
        var properties = new List<EntityProperty>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = dbFlavor == DbFlavor.Oracle ? OracleColumnQuery : AnsiColumnQuery;
        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":schema"    : "@schema",    schema);
        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":tableName" : "@tableName", tableName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var colName  = reader.GetString(0);
            var dataType = reader.GetString(1);
            // Oracle: nullable='Y' means nullable; ANSI: is_nullable='YES' means nullable
            var nullable = dbFlavor == DbFlavor.Oracle
                ? reader.GetString(2).Equals("Y", StringComparison.OrdinalIgnoreCase)
                : reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase);

            if (pkColumns.Contains(colName))
                continue;

            var csType   = SqlTypeMapper.ToCSharpType(dataType, dbFlavor);
            var propName = SqlTypeMapper.ToPascalCase(colName);

            properties.Add(new EntityProperty(propName, csType, !nullable));
        }

        return properties;
    }

    private static HashSet<string> ReadPrimaryKeys(DbConnection conn, string schema, string tableName, DbFlavor dbFlavor)
    {
        var pks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = dbFlavor == DbFlavor.Oracle ? OraclePkQuery : AnsiPkQuery;
        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":schema"    : "@schema",    schema);
        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":tableName" : "@tableName", tableName);

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
