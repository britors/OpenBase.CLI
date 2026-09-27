namespace OpenBase.CLI.Helpers.Execution;

public static class PackageIds
{
    public const string Unified = "w3ti.OpenBaseNET.Template";
    public const string Cli       = "w3ti.OpenBase.CLI";
    public const string SqlServer = "w3ti.OpenBaseNET.SQLServer.Template";
    public const string Postgres  = "w3ti.OpenBaseNET.Postgres.Template";
    public const string Oracle    = "w3ti.OpenBaseNET.Oracle.Template";

    public static readonly string[] Templates = [Unified];
    public static readonly string[] All       = [Cli, Unified, SqlServer, Postgres, Oracle];

    public static readonly IReadOnlyDictionary<string, string> TypeToId =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cli"]       = Cli,
            ["template"] = Unified,
            ["sqlserver"] = SqlServer,
            ["postgres"]  = Postgres,
            ["oracle"]    = Oracle,
        };

    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Cli]       = "OpenBase CLI",
            [Unified] = "Template OpenBaseNET",
            [SqlServer] = "Template SQLServer",
            [Postgres]  = "Template Postgres",
            [Oracle]    = "Template Oracle",
        };
}
