using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class DbFlavorDetector : IDbFlavorDetector
{
    private const string NpgsqlPackage = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public DbFlavor Detect(string solutionDir)
    {
        var srcDir = Path.Combine(solutionDir, "src");
        if (!Directory.Exists(srcDir))
            return DbFlavor.SqlServer;

        foreach (var csproj in Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories))
        {
            if (File.ReadAllText(csproj).Contains(NpgsqlPackage, StringComparison.OrdinalIgnoreCase))
                return DbFlavor.Postgres;
        }

        return DbFlavor.SqlServer;
    }
}
