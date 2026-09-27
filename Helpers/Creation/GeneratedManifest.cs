using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OpenBase.CLI.Helpers.Execution;

namespace OpenBase.CLI.Helpers.Creation;

public sealed record GeneratedManifest(string ApiProject, string ConnectionKey)
{
    public static GeneratedManifest Read(string root, string database, string name, string version)
    {
        try
        {
            var manifestPath = Resolve(root, ".openbase.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var m = doc.RootElement;
            if (m.TryGetProperty("schemaVersion", out var sv) && sv.ValueKind == JsonValueKind.Number && sv.GetInt32() != 2)
                throw new CliException("MANIFEST_VERSION_UNSUPPORTED", "Versão de manifesto não suportada.");
            Shape(m, ["schemaVersion", "template", "templateVersion", "architecture", "database", "rootNamespace", "solution", "projects", "persistence"], ["$schema", "cliVersion"]);
            Require(m.GetProperty("schemaVersion").GetInt32() == 2);
            Require(Text(m, "template") == "openbasenet" && Text(m, "architecture") == "hexagonal");
            Require(Text(m, "database") == database && Text(m, "rootNamespace") == name && Identifiers.IsName(name));
            Require(PackageVersion.IsValid(Text(m, "templateVersion")) && new PackageVersion(Text(m, "templateVersion")).CompareTo(new PackageVersion(version)) == 0);
            if (m.TryGetProperty("cliVersion", out var cv)) Require(PackageVersion.IsValid(cv.GetString()));
            if (m.TryGetProperty("$schema", out var schema)) Require(schema.GetString() == "https://raw.githubusercontent.com/britors/OpenBaseNET/main/contracts/openbase.schema.json");
            var solution = Resolve(root, Text(m, "solution"));
            Require(File.Exists(solution) && (solution.EndsWith(".sln", StringComparison.Ordinal) || solution.EndsWith(".slnx", StringComparison.Ordinal)));
            var projects = m.GetProperty("projects");
            Shape(projects, ["domain", "application", "infrastructure", "api"], []);
            var paths = projects.EnumerateObject().Select(p => Resolve(root, p.Value.GetString()!)).ToArray();
            Require(paths.Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).Count() == 4);
            var solutionText = File.ReadAllText(solution).Replace('\\', '/');
            foreach (var p in paths)
            {
                Require(p.EndsWith(".csproj", StringComparison.Ordinal) && File.Exists(p));
                var relative = Path.GetRelativePath(root, p).Replace('\\', '/');
                Require(solutionText.Contains('"' + relative + '"', StringComparison.Ordinal));
                _ = XDocument.Load(p);
            }
            var persistence = m.GetProperty("persistence");
            Shape(persistence, ["dbContext", "migrationsPath", "connectionStringName"], []);
            Require(Identifiers.IsName(Text(persistence, "dbContext")));
            var migration = Resolve(root, Text(persistence, "migrationsPath"));
            var infrastructure = Resolve(root, Text(projects, "infrastructure"));
            Require(migration.StartsWith(Path.GetDirectoryName(infrastructure)! + Path.DirectorySeparatorChar, PathComparison));
            var key = Text(persistence, "connectionStringName");
            Require(Regex.IsMatch(key, @"\A[A-Za-z_][A-Za-z0-9_.-]*\z", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1)));
            // Generated variants have unconditional references. Do not infer an engine from conditional MSBuild text.
            var references = XDocument.Load(infrastructure).Descendants("PackageReference").ToArray();
            var drivers = new Dictionary<string, string> { ["Npgsql.EntityFrameworkCore.PostgreSQL"] = "postgres", ["Microsoft.EntityFrameworkCore.SqlServer"] = "sqlserver", ["Oracle.EntityFrameworkCore"] = "oracle" };
            var selected = references.Where(p => drivers.ContainsKey((string?)p.Attribute("Include") ?? "")).ToArray();
            Require(selected.Length == 1 && drivers[(string)selected[0].Attribute("Include")!] == database);
            Require(!selected[0].AncestorsAndSelf().Any(p => p.Attribute("Condition") is not null));
            return new(Resolve(root, Text(projects, "api")), key);
        }
        catch (CliException) { throw; }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException or KeyNotFoundException or ArgumentException or System.Xml.XmlException or FormatException or OverflowException)
        { throw new CliException("MANIFEST_INVALID", "O manifesto gerado ou seus caminhos são inválidos."); }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static string Text(JsonElement item, string key) => item.GetProperty(key).GetString() ?? throw new InvalidOperationException();
    private static void Require(bool condition) { if (!condition) throw new CliException("MANIFEST_INVALID", "O manifesto gerado não corresponde ao contrato do template."); }
    private static void Shape(JsonElement value, string[] required, string[] optional)
    {
        var names = value.EnumerateObject().Select(p => p.Name).ToArray();
        Require(names.Distinct().Count() == names.Length && required.All(names.Contains) && names.All(n => required.Contains(n) || optional.Contains(n)));
    }

    public static string Resolve(string root, string relative)
    {
        Require(!string.IsNullOrEmpty(relative) && !Path.IsPathRooted(relative) && !relative.Any(c => c is '\\' or ':' || char.IsControl(c)));
        var parts = relative.Split('/');
        Require(parts.All(p => p is not ("" or "." or "..")));
        // Reject links rather than following a template path outside the selected output directory.
        var path = Path.GetFullPath(root);
        foreach (var part in parts)
        {
            path = Path.Combine(path, part);
            if (File.Exists(path) || Directory.Exists(path)) Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0);
            else Require(new FileInfo(path).LinkTarget is null);
        }
        return path;
    }
}
