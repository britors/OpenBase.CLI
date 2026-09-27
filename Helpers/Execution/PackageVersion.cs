using System.Numerics;
using System.Text.RegularExpressions;

namespace OpenBase.CLI.Helpers.Execution;

// SemVer ordering; numeric prerelease identifiers compare numerically, releases sort last.
public sealed record PackageVersion(string Value) : IComparable<PackageVersion>
{
    private static readonly Regex Pattern = new(@"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-((?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1));
    public static bool IsValid(string? value) => value is not null && Pattern.IsMatch(value);
    public bool Preview => Pattern.Match(Value).Groups[4].Success;
    public bool Compatible => IsValid(Value) && Pattern.Match(Value).Groups[1].Value == "11";
    public int CompareTo(PackageVersion? other)
    {
        if (other is null) return 1;
        var a = Pattern.Match(Value); var b = Pattern.Match(other.Value);
        for (var i = 1; i <= 3; i++)
        {
            var c = BigInteger.Parse(a.Groups[i].Value).CompareTo(BigInteger.Parse(b.Groups[i].Value));
            if (c != 0) return c;
        }
        if (!Preview || !other.Preview) return Preview == other.Preview ? 0 : Preview ? -1 : 1;
        var aa = a.Groups[4].Value.Split('.'); var bb = b.Groups[4].Value.Split('.');
        for (var i = 0; i < Math.Min(aa.Length, bb.Length); i++)
        {
            var an = BigInteger.TryParse(aa[i], out var av); var bn = BigInteger.TryParse(bb[i], out var bv);
            var c = an && bn ? av.CompareTo(bv) : an != bn ? an ? -1 : 1 : string.CompareOrdinal(aa[i], bb[i]);
            if (c != 0) return c;
        }
        return aa.Length.CompareTo(bb.Length);
    }
}
