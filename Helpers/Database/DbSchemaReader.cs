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
        {
            var tableName = reader.GetString(1);
            if (tableName.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
                continue;
            tables.Add(new DbTableInfo(reader.GetString(0), tableName));
        }

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


    private const string SqlServerListProceduresQuery = """
        SELECT SCHEMA_NAME(schema_id) AS schema_name, name AS proc_name
        FROM sys.objects
        WHERE type IN ('P', 'FN', 'IF', 'TF')
          AND is_ms_shipped = 0
        ORDER BY schema_name, proc_name
        """;

    private const string OracleListProceduresQuery = """
        SELECT owner, object_name
        FROM all_objects
        WHERE object_type IN ('PROCEDURE', 'FUNCTION', 'PACKAGE')
          AND owner NOT IN (
              'SYS', 'SYSTEM', 'OUTLN', 'DBSNMP', 'ORACLE_OCM', 'MDSYS', 'ORDSYS',
              'ORDDATA', 'XDB', 'WMSYS', 'CTXSYS', 'EXFSYS', 'DVSYS', 'LBACSYS',
              'OJVMSYS', 'OLAPSYS', 'APPQOSSYS', 'AUDSYS', 'GSMADMIN_INTERNAL',
              'XS$NULL', 'GGSYS', 'GSMCATUSER', 'GSMUSER', 'SYSRAC', 'DVF',
              'SYSBACKUP', 'SYSDG', 'SYSKM', 'DBSFWUSER', 'REMOTE_SCHEDULER_AGENT'
          )
        ORDER BY owner, object_name
        """;

    private const string PostgresListProceduresQuery = """
        SELECT routine_schema, routine_name
        FROM information_schema.routines
        WHERE routine_type IN ('PROCEDURE', 'FUNCTION')
          AND routine_schema NOT IN ('pg_catalog', 'information_schema')
        ORDER BY routine_schema, routine_name
        """;

    public IReadOnlyList<DbProcedureInfo> ListProcedures(string connectionString, DbFlavor dbFlavor)
    {
        using DbConnection conn = dbFlavor switch
        {
            DbFlavor.Postgres => new NpgsqlConnection(connectionString),
            DbFlavor.Oracle   => new OracleConnection(connectionString),
            _                 => new SqlConnection(connectionString),
        };
        conn.Open();

        var procs = new List<DbProcedureInfo>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = dbFlavor switch
        {
            DbFlavor.Oracle   => OracleListProceduresQuery,
            DbFlavor.Postgres => PostgresListProceduresQuery,
            _                 => SqlServerListProceduresQuery,
        };

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            procs.Add(new DbProcedureInfo(reader.GetString(0), reader.GetString(1)));

        return procs;
    }


    private const string SqlServerProcParamsQuery = """
        SELECT p.name, t.name AS data_type, p.is_output
        FROM sys.parameters p
        JOIN sys.objects o ON p.object_id = o.object_id
        JOIN sys.types t ON p.user_type_id = t.user_type_id
        WHERE SCHEMA_NAME(o.schema_id) = @schema
          AND o.name = @procedureName
          AND p.parameter_id > 0
        ORDER BY p.parameter_id
        """;

    private const string OracleProcParamsQuery = """
        SELECT argument_name, data_type, in_out
        FROM all_arguments
        WHERE owner = UPPER(:schema)
          AND object_name = UPPER(:procedureName)
          AND argument_name IS NOT NULL
        ORDER BY position
        """;

    private const string PostgresProcParamsQuery = """
        SELECT p.parameter_name, p.data_type, p.parameter_mode
        FROM information_schema.parameters p
        JOIN information_schema.routines r
            ON p.specific_schema = r.specific_schema
           AND p.specific_name   = r.specific_name
        WHERE r.routine_schema = @schema
          AND r.routine_name   = @procedureName
        ORDER BY p.ordinal_position
        """;

    public IReadOnlyList<ProcedureParameter> ReadProcedureParameters(
        string connectionString,
        string schema,
        string procedureName,
        DbFlavor dbFlavor)
    {
        using DbConnection conn = dbFlavor switch
        {
            DbFlavor.Postgres => new NpgsqlConnection(connectionString),
            DbFlavor.Oracle   => new OracleConnection(connectionString),
            _                 => new SqlConnection(connectionString),
        };
        conn.Open();

        var parameters = new List<ProcedureParameter>();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = dbFlavor switch
        {
            DbFlavor.Oracle   => OracleProcParamsQuery,
            DbFlavor.Postgres => PostgresProcParamsQuery,
            _                 => SqlServerProcParamsQuery,
        };

        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":schema"        : "@schema",        schema);
        AddParam(cmd, dbFlavor == DbFlavor.Oracle ? ":procedureName" : "@procedureName", procedureName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var rawName  = reader.GetString(0).TrimStart('@');
            var dataType = reader.GetString(1);
            var dirRaw   = reader.GetString(2);

            var name   = SqlTypeMapper.ToPascalCase(rawName);
            var csType = SqlTypeMapper.ToCSharpType(dataType, dbFlavor);
            var dir    = ParseDirection(dirRaw, dbFlavor);

            parameters.Add(new ProcedureParameter(name, csType, dir));
        }

        return parameters;
    }

    private static Models.ParameterDirection ParseDirection(string raw, DbFlavor dbFlavor) =>
        dbFlavor switch
        {
            DbFlavor.SqlServer => raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                ? Models.ParameterDirection.Out
                : Models.ParameterDirection.In,
            DbFlavor.Oracle => raw.Equals("IN/OUT", StringComparison.OrdinalIgnoreCase)
                ? Models.ParameterDirection.InOut
                : raw.Equals("OUT", StringComparison.OrdinalIgnoreCase)
                    ? Models.ParameterDirection.Out
                    : Models.ParameterDirection.In,
            _ => raw.Equals("INOUT", StringComparison.OrdinalIgnoreCase)
                ? Models.ParameterDirection.InOut
                : raw.Equals("OUT", StringComparison.OrdinalIgnoreCase)
                    ? Models.ParameterDirection.Out
                    : Models.ParameterDirection.In,
        };
}
