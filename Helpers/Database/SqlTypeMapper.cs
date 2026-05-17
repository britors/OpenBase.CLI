using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public static class SqlTypeMapper
{
    public static string ToCSharpType(string sqlType, DbFlavor flavor)
    {
        var t = sqlType.ToLowerInvariant().Trim();
        return t switch
        {
            "int" or "int4" or "integer"                                          => "int",
            "bigint" or "int8"                                                    => "long",
            "smallint" or "int2" or "tinyint"                                     => "short",
            "nvarchar" or "varchar" or "char" or "nchar" or "text" or "ntext"
                or "character varying" or "character" or "bpchar"                 => "string",
            "bit" or "boolean" or "bool"                                          => "bool",
            "decimal" or "numeric" or "money" or "smallmoney"                     => "decimal",
            "float" or "float8" or "double precision"                             => "double",
            "real" or "float4"                                                     => "float",
            "datetime" or "datetime2" or "smalldatetime"
                or "timestamp" or "timestamp without time zone"                   => "DateTime",
            "date"                                                                 => "DateOnly",
            "time" or "time without time zone"                                    => "TimeOnly",
            "datetimeoffset" or "timestamp with time zone" or "timestamptz"       => "DateTimeOffset",
            "uniqueidentifier" or "uuid"                                          => "Guid",
            "varbinary" or "binary" or "image" or "bytea"                        => "byte[]",
            "json" or "jsonb"                                                     => "JsonDocument",

            // Oracle
            "number"                                                              => "decimal",
            "varchar2" or "nvarchar2" or "nchar"
                or "clob" or "nclob"                                             => "string",
            "binary_float"                                                        => "float",
            "binary_double"                                                       => "double",
            "blob" or "raw" or "long raw"                                         => "byte[]",
            "timestamp with time zone" or "timestamp with local time zone"        => "DateTimeOffset",

            _                                                                     => "string"
        };
    }

    public static string ToPascalCase(string columnName)
    {
        if (columnName.Contains('_'))
        {
            return string.Concat(columnName
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        }

        return char.ToUpperInvariant(columnName[0]) + columnName[1..];
    }
}
