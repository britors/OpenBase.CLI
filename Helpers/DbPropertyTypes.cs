using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

public static class DbPropertyTypes
{
    private static readonly IReadOnlyList<string> CommonTypes =
    [
        "int", "long", "short", "string", "bool", "decimal", "float", "double",
        "DateTime", "DateOnly", "TimeOnly", "DateTimeOffset", "Guid", "byte[]"
    ];

    private static readonly IReadOnlyList<string> PostgresExtras = ["JsonDocument"];

    public static IReadOnlyList<string> GetValidTypes(DbFlavor flavor) =>
        flavor == DbFlavor.Postgres
            ? [.. CommonTypes, .. PostgresExtras]
            : CommonTypes;

    public static string GetTestValue(EntityProperty prop) => prop.CsType switch
    {
        "string"         => "\"Test\"",
        "int"            => "1",
        "long"           => "1L",
        "short"          => "(short)1",
        "bool"           => "true",
        "decimal"        => "1.0m",
        "float"          => "1.0f",
        "double"         => "1.0d",
        "DateTime"       => "DateTime.Now",
        "DateOnly"       => "DateOnly.FromDateTime(DateTime.Now)",
        "TimeOnly"       => "TimeOnly.MinValue",
        "DateTimeOffset" => "DateTimeOffset.Now",
        "Guid"           => "Guid.NewGuid()",
        "byte[]"         => "[]",
        "JsonDocument"   => "JsonDocument.Parse(\"{}\")",
        _                => "default"
    };

    public static bool HasStableTestValue(EntityProperty prop) =>
        prop.IsRequired && prop.CsType is "string" or "int" or "long" or "short" or "bool" or "decimal";
}
