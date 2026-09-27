using System.Text.RegularExpressions;

namespace OpenBase.CLI.Helpers.Creation;

public static class Identifiers
{
    private static readonly HashSet<string> Keywords = new(("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while __arglist __makeref __reftype __refvalue").Split(' '));
    public static bool IsName(string value) => Regex.IsMatch(value, @"\A[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*\z")
        && value.Split('.').All(p => !Keywords.Contains(p));

    public static string Database(string value)
    {
        // Spectre 0.55 supplies this sentinel for a missing array-option value.
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith('-') || value == "__default_command")
            throw new CliException("ARGUMENT_INVALID", "Informe um valor para a opção de banco.");
        return value.ToLowerInvariant() switch
        {
        "postgres" or "postgresql" or "pgsql" => "postgres",
        "sqlserver" => "sqlserver", "oracle" => "oracle",
        _ => throw new CliException("DATABASE_UNSUPPORTED", "Use postgres, sqlserver ou oracle.")
        };
    }
}
