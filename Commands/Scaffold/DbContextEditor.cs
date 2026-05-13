using OpenBase.CLI.Helpers.IO;

namespace OpenBase.CLI.Commands.Scaffold;

public enum DbSetInjectionResult { Injected, AlreadyExists, FileNotFound, Failed }

public sealed class DbContextEditor(IFileWriter fileWriter)
{
    private const string DbContextFileName = "OneBaseDataBaseContext.cs";

    public DbSetInjectionResult InjectDbSet(ScaffoldContext ctx)
    {
        var path = Path.Combine(ctx.InfraContextPath, DbContextFileName);
        if (!fileWriter.FileExists(path))
            return DbSetInjectionResult.FileNotFound;

        try
        {
            var content = fileWriter.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content))
                return DbSetInjectionResult.Failed;

            if (content.Contains($"DbSet<{ctx.Entity}>"))
                return DbSetInjectionResult.AlreadyExists;

            var sep   = content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();

            EnsureEntityUsing(lines, content, ctx.NS);

            var dbSetLine = $"    public DbSet<{ctx.Entity}> {ctx.EPlural} {{ get; set; }}";
            if (!TryInsertDbSetLine(lines, dbSetLine))
                return DbSetInjectionResult.Failed;

            fileWriter.WriteAllText(path, string.Join(sep, lines));
            return DbSetInjectionResult.Injected;
        }
        catch
        {
            return DbSetInjectionResult.Failed;
        }
    }

    public static string EmptyMigrationUpMethod(string content)
    {
        const string upSignature = "protected override void Up(MigrationBuilder migrationBuilder)";
        var signatureIdx = content.IndexOf(upSignature, StringComparison.Ordinal);
        if (signatureIdx < 0) return content;

        var openBraceIdx = content.IndexOf('{', signatureIdx + upSignature.Length);
        if (openBraceIdx < 0) return content;

        var depth = 1;
        var i = openBraceIdx + 1;
        while (i < content.Length && depth > 0)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}') depth--;
            i++;
        }
        if (depth != 0) return content;

        var closeBraceIdx = i - 1;
        return content[..(openBraceIdx + 1)] + "\n        " + content[closeBraceIdx..];
    }

    private static void EnsureEntityUsing(List<string> lines, string content, string ns)
    {
        var entitiesUsing = $"using {ns}.Domain.Entities;";
        if (content.Contains(entitiesUsing)) return;

        var lastUsing = lines.FindLastIndex(l => l.TrimStart().StartsWith("using "));
        if (lastUsing >= 0)
            lines.Insert(lastUsing + 1, entitiesUsing);
        else
            lines.Insert(0, entitiesUsing);
    }

    private static bool TryInsertDbSetLine(List<string> lines, string dbSetLine)
    {
        var lastDbSet = lines.FindLastIndex(l => l.Contains("DbSet<"));
        if (lastDbSet >= 0)
        {
            lines.Insert(lastDbSet + 1, dbSetLine);
            return true;
        }

        var classIdx = lines.FindIndex(l => l.Contains("class OneBaseDataBaseContext"));
        if (classIdx < 0) return false;

        var braceIdx = lines.FindIndex(classIdx, l => l.Trim() == "{");
        if (braceIdx < 0) return false;

        lines.Insert(braceIdx + 1, dbSetLine);
        lines.Insert(braceIdx + 2, string.Empty);
        return true;
    }
}
