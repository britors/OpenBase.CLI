using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public static class SqlTypeMapper
{
    private const string CsString = "string";

    private static readonly IReadOnlyDictionary<string, string> TypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // integer
            ["int"] = "int", ["int4"] = "int", ["integer"] = "int",
            ["bigint"] = "long", ["int8"] = "long",
            ["smallint"] = "short", ["int2"] = "short", ["tinyint"] = "short",
            // string
            ["nvarchar"] = CsString, ["varchar"] = CsString, ["char"] = CsString,
            ["nchar"] = CsString, ["text"] = CsString, ["ntext"] = CsString,
            ["character varying"] = CsString, ["character"] = CsString, ["bpchar"] = CsString,
            ["varchar2"] = CsString, ["nvarchar2"] = CsString, ["clob"] = CsString, ["nclob"] = CsString,
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
        return TypeMap.TryGetValue(t, out var csType) ? csType : CsString;
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
