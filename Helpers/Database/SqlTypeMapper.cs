using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public static class SqlTypeMapper
{
    private static readonly IReadOnlyDictionary<string, string> TypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // integer
            ["int"] = "int", ["int4"] = "int", ["integer"] = "int",
            ["bigint"] = "long", ["int8"] = "long",
            ["smallint"] = "short", ["int2"] = "short", ["tinyint"] = "short",
            // string
            ["nvarchar"] = "string", ["varchar"] = "string", ["char"] = "string",
            ["nchar"] = "string", ["text"] = "string", ["ntext"] = "string",
            ["character varying"] = "string", ["character"] = "string", ["bpchar"] = "string",
            ["varchar2"] = "string", ["nvarchar2"] = "string", ["clob"] = "string", ["nclob"] = "string",
            // bool
            ["bit"] = "bool", ["boolean"] = "bool", ["bool"] = "bool",
            // decimal
            ["decimal"] = "decimal", ["numeric"] = "decimal", ["money"] = "decimal",
            ["smallmoney"] = "decimal", ["number"] = "decimal",
            // double
            ["float"] = "double", ["float8"] = "double", ["double precision"] = "double",
            ["binary_double"] = "double",
            // float
            ["real"] = "float", ["float4"] = "float", ["binary_float"] = "float",
            // DateTime
            ["datetime"] = "DateTime", ["datetime2"] = "DateTime", ["smalldatetime"] = "DateTime",
            ["timestamp"] = "DateTime", ["timestamp without time zone"] = "DateTime",
            // DateOnly
            ["date"] = "DateOnly",
            // TimeOnly
            ["time"] = "TimeOnly", ["time without time zone"] = "TimeOnly",
            // DateTimeOffset
            ["datetimeoffset"] = "DateTimeOffset", ["timestamptz"] = "DateTimeOffset",
            ["timestamp with time zone"] = "DateTimeOffset",
            ["timestamp with local time zone"] = "DateTimeOffset",
            // Guid
            ["uniqueidentifier"] = "Guid", ["uuid"] = "Guid",
            // byte[]
            ["varbinary"] = "byte[]", ["binary"] = "byte[]", ["image"] = "byte[]",
            ["bytea"] = "byte[]", ["blob"] = "byte[]", ["raw"] = "byte[]", ["long raw"] = "byte[]",
            // JsonDocument
            ["json"] = "JsonDocument", ["jsonb"] = "JsonDocument",
        };

    public static string ToCSharpType(string sqlType, DbFlavor flavor)
    {
        var t = sqlType.ToLowerInvariant().Trim();
        return TypeMap.TryGetValue(t, out var csType) ? csType : "string";
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
