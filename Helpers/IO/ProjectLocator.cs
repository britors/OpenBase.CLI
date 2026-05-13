using System.Diagnostics.CodeAnalysis;

namespace OpenBase.CLI.Helpers.IO;

[ExcludeFromCodeCoverage]
public sealed class ProjectLocator : IProjectLocator
{
    public (string? SolutionDir, string? RootNamespace) Detect(string workingDir, string? overrideNamespace)
    {
        var dir = new DirectoryInfo(workingDir);

        for (var depth = 0; depth < 6 && dir is not null; depth++, dir = dir.Parent)
        {
            if (Directory.GetFiles(dir.FullName, "*.sln", SearchOption.TopDirectoryOnly).Length == 0)
                continue;

            var srcDir = Path.Combine(dir.FullName, "src");
            if (!Directory.Exists(srcDir))
                break;

            var domainDir = Directory.GetDirectories(srcDir, "*.Domain", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (domainDir is null)
                break;

            var folderName = Path.GetFileName(domainDir);
            var ns = overrideNamespace ?? folderName[..^7];
            return (dir.FullName, ns);
        }

        return (null, null);
    }
}
