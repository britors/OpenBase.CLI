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

        var hasNpgsql = Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(NpgsqlPackage, StringComparison.OrdinalIgnoreCase))
            .Any();

        return hasNpgsql ? DbFlavor.Postgres : DbFlavor.SqlServer;
    }
}
