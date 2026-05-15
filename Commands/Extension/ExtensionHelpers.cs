using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension;

internal static class ExtensionHelpers
{
    internal static void WriteFiles(
        IEnumerable<(string Path, string Content)> files,
        string solutionDir,
        IFileWriter fileWriter,
        IAnsiConsole console)
    {
        foreach (var (path, content) in files)
        {
            if (fileWriter.FileExists(path))
            {
                console.MarkupLine(string.Format(SR.Current.ExtensionFileSkipped, Path.GetFileName(path)));
                continue;
            }
            fileWriter.EnsureDirectory(Path.GetDirectoryName(path)!);
            fileWriter.WriteAllText(path, content);
            console.MarkupLine(string.Format(SR.Current.ExtensionFileCreated, Path.GetRelativePath(solutionDir, path)));
        }
    }

    internal static string InjectPresentationUsing(string content, string ns)
    {
        var usingDirective = $"using {ns}.Presentation.Api.Extensions;";
        if (content.Contains(usingDirective)) return content;
        return content.Insert(0, $"{usingDirective}\n");
    }

    internal static string InsertBeforeAnchor(string content, string anchor, string toInsert)
    {
        var idx = content.IndexOf(anchor, StringComparison.Ordinal);
        return idx >= 0 ? content.Insert(idx, toInsert) : content;
    }

    internal static string InsertAfterLine(string content, string anchor, string toInsert)
    {
        var idx = content.IndexOf(anchor, StringComparison.Ordinal);
        if (idx < 0) return content;
        var afterLine = SkipNewLine(content, idx + anchor.Length);
        return content.Insert(afterLine, $"{toInsert}\n");
    }

    internal static int SkipNewLine(string content, int idx)
    {
        if (idx < content.Length && content[idx] == '\r') idx++;
        if (idx < content.Length && content[idx] == '\n') idx++;
        return idx;
    }
}
