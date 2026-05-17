using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public static class SqlTypeMapper
{
    private const string CsString          = "string";
    private const string CsInt             = "int";
    private const string CsLong            = "long";
    private const string CsShort           = "short";
    private const string CsBool            = "bool";
    private const string CsDecimal         = "decimal";
    private const string CsDouble          = "double";
    private const string CsFloat           = "float";
    private const string CsDateTime        = "DateTime";
    private const string CsDateTimeOffset  = "DateTimeOffset";
    private const string CsByteArray       = "byte[]";

    private static readonly IReadOnlyDictionary<string, string> TypeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // integer
            ["int"] = CsInt, ["int4"] = CsInt, ["integer"] = CsInt,
            ["bigint"] = CsLong, ["int8"] = CsLong,
            ["smallint"] = CsShort, ["int2"] = CsShort, ["tinyint"] = CsShort,
            // string
            ["nvarchar"] = CsString, ["varchar"] = CsString, ["char"] = CsString,
            ["nchar"] = CsString, ["text"] = CsString, ["ntext"] = CsString,
            ["character varying"] = CsString, ["character"] = CsString, ["bpchar"] = CsString,
            ["varchar2"] = CsString, ["nvarchar2"] = CsString, ["clob"] = CsString, ["nclob"] = CsString,
            // bool
            ["bit"] = CsBool, ["boolean"] = CsBool, ["bool"] = CsBool,
            // decimal
            ["decimal"] = CsDecimal, ["numeric"] = CsDecimal, ["money"] = CsDecimal,
            ["smallmoney"] = CsDecimal, ["number"] = CsDecimal,
            // double
            ["float"] = CsDouble, ["float8"] = CsDouble, ["double precision"] = CsDouble,
            ["binary_double"] = CsDouble,
            // float
            ["real"] = CsFloat, ["float4"] = CsFloat, ["binary_float"] = CsFloat,
            // DateTime
            ["datetime"] = CsDateTime, ["datetime2"] = CsDateTime, ["smalldatetime"] = CsDateTime,
            ["timestamp"] = CsDateTime, ["timestamp without time zone"] = CsDateTime,
            // DateOnly
            ["date"] = "DateOnly",
            // TimeOnly
            ["time"] = "TimeOnly", ["time without time zone"] = "TimeOnly",
            // DateTimeOffset
            ["datetimeoffset"] = CsDateTimeOffset, ["timestamptz"] = CsDateTimeOffset,
            ["timestamp with time zone"] = CsDateTimeOffset,
            ["timestamp with local time zone"] = CsDateTimeOffset,
            // Guid
            ["uniqueidentifier"] = "Guid", ["uuid"] = "Guid",
            // byte[]
            ["varbinary"] = CsByteArray, ["binary"] = CsByteArray, ["image"] = CsByteArray,
            ["bytea"] = CsByteArray, ["blob"] = CsByteArray, ["raw"] = CsByteArray, ["long raw"] = CsByteArray,
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
