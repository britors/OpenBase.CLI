using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

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
            .Any(f => File.ReadAllText(f).Contains(NpgsqlPackage, StringComparison.OrdinalIgnoreCase));

        return hasNpgsql ? DbFlavor.Postgres : DbFlavor.SqlServer;
    }
}
