using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Scaffold;

internal sealed class ScaffoldFileHelper(IAnsiConsole console, IFileWriter fileWriter)
{
    public (List<string> Created, List<string> Skipped, List<string> Failed) WriteFiles(
        IEnumerable<(string Path, string Content)> files,
        string solutionDir,
        string entityName)
    {
        var created = new List<string>();
        var skipped = new List<string>();
        var failed  = new List<string>();

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(string.Format(SR.Current.GeneratingScaffold, entityName), _ =>
            {
                foreach (var (path, content) in files)
                {
                    var rel = Path.GetRelativePath(solutionDir, path);
                    try
                    {
                        fileWriter.EnsureDirectory(Path.GetDirectoryName(path)!);

                        if (fileWriter.FileExists(path))
                        {
                            skipped.Add(rel);
                            continue;
                        }

                        fileWriter.WriteAllText(path, content);
                        created.Add(rel);
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{rel}: {ex.Message}");
                    }
                }
            });

        return (created, skipped, failed);
    }

    public void PrintFileList(string header, List<string> files, string headerColor, string fileColor = "grey")
    {
        if (files.Count == 0) return;
        console.MarkupLine($"\n[{headerColor}]{header}[/]");
        foreach (var f in files)
            console.MarkupLine($"  [{fileColor}]{Markup.Escape(f)}[/]");
    }
}
