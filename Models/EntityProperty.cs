namespace OpenBase.CLI.Models;

public enum DbFlavor { SqlServer, Postgres, Oracle }

public sealed record EntityProperty(string Name, string CsType, bool IsRequired)
{
    public string? DbColumnName { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string CamelName => char.ToLowerInvariant(Name[0]) + Name[1..];
    public string ActualCsType => IsRequired ? CsType : CsType + "?";
    public bool IsStringType => CsType == "string";
}
