using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public sealed class DbFlavorDetector : IDbFlavorDetector
{
    private const string NpgsqlPackage  = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string OraclePackage  = "Oracle.EntityFrameworkCore";

    public DbFlavor Detect(string solutionDir)
    {
        var srcDir = Path.Combine(solutionDir, "src");
        if (!Directory.Exists(srcDir))
            return DbFlavor.SqlServer;

        var csprojContents = Directory
            .GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToList();

        if (csprojContents.Any(c => c.Contains(OraclePackage, StringComparison.OrdinalIgnoreCase)))
            return DbFlavor.Oracle;

        if (csprojContents.Any(c => c.Contains(NpgsqlPackage, StringComparison.OrdinalIgnoreCase)))
            return DbFlavor.Postgres;

        return DbFlavor.SqlServer;
    }
}
