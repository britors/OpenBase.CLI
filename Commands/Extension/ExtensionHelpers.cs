using System.Text.Json;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension;

internal record ProgramCsMessages(
    string NotFound,
    string AlreadyConfigured,
    string Injected,
    string Warning);

internal record InfraProjectMessages(
    string Created,
    string AlreadyExists,
    string Failed);

internal static class ExtensionHelpers
{
    internal static (string Ns, string SolutionDir, string AppPath, string InfraDataPath, string PresentationPath)?
        ResolveSolutionPaths(ExtensionContext context)
    {
        if (context.SolutionDir is null || context.RootNamespace is null) return null;
        var ns = context.RootNamespace;
        var src = Path.Combine(context.SolutionDir, "src");
        return (ns, context.SolutionDir,
            Path.Combine(src, $"{ns}.Application"),
            Path.Combine(src, $"{ns}.Infra.Data"),
            Path.Combine(src, $"{ns}.Presentation.Api"));
    }

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

    internal static void AddProjectReference(
        string csprojPath,
        string referencePath,
        IFileWriter fileWriter,
        IDotNetRunner dotNetRunner,
        IAnsiConsole console)
    {
        if (!fileWriter.FileExists(csprojPath)) return;
        var content = fileWriter.ReadAllText(csprojPath);
        if (content.Contains(Path.GetFileName(referencePath))) return;

        console.MarkupLine(string.Format(SR.Current.ExtensionAddingReference,
            Path.GetFileName(referencePath), Path.GetFileName(csprojPath)));
        var (ok, err) = dotNetRunner.Run($"add \"{csprojPath}\" reference \"{referencePath}\"");
        if (!ok)
            console.MarkupLine(string.Format(SR.Current.ExtensionReferenceAddWarning,
                Path.GetFileName(referencePath), err));
    }

    internal static bool AddPackage(
        string csprojPath,
        string packageId,
        IFileWriter fileWriter,
        IDotNetRunner dotNetRunner,
        IAnsiConsole console)
    {
        if (!fileWriter.FileExists(csprojPath)) return true;
        var content = fileWriter.ReadAllText(csprojPath);
        if (content.Contains(packageId)) return true;

        console.MarkupLine(string.Format(SR.Current.ExtensionAddingPackage, packageId, Path.GetFileName(csprojPath)));
        var (ok, err) = dotNetRunner.Run($"add \"{csprojPath}\" package {packageId}");
        if (!ok)
            console.MarkupLine(string.Format(SR.Current.ExtensionPackageAddWarning, packageId, err));
        return ok;
    }

    internal static void InjectProgramCs(
        string presentationPath,
        IFileWriter fileWriter,
        IAnsiConsole console,
        ProgramCsMessages messages,
        Func<string, bool> isConfigured,
        Func<string, string> transform)
    {
        var path = Path.Combine(presentationPath, "Program.cs");
        if (!fileWriter.FileExists(path))
        {
            console.MarkupLine(messages.NotFound);
            return;
        }

        try
        {
            var content = fileWriter.ReadAllText(path);
            if (isConfigured(content))
            {
                console.MarkupLine(messages.AlreadyConfigured);
                return;
            }

            content = transform(content);
            fileWriter.WriteAllText(path, content);
            console.MarkupLine(messages.Injected);
        }
        catch (Exception ex)
        {
            console.MarkupLine(string.Format(messages.Warning, ex.Message));
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

    internal static bool CreateDedicatedProject(
        string ns,
        string projectSuffix,
        string solutionDir,
        string projectPath,
        string appPath,
        string presentationPath,
        IReadOnlyList<string> packages,
        InfraProjectMessages messages,
        IFileWriter fileWriter,
        IDotNetRunner dotNetRunner,
        IAnsiConsole console,
        string? solutionFolder = null)
    {
        var projectCsproj      = Path.Combine(projectPath, $"{ns}.{projectSuffix}.csproj");
        var appCsproj          = Path.Combine(appPath, $"{ns}.Application.csproj");
        var presentationCsproj = Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj");
        var projectName        = $"{ns}.{projectSuffix}";

        if (!fileWriter.FileExists(projectCsproj))
        {
            var (ok, err) = dotNetRunner.Run($"new classlib -n \"{projectName}\" -o \"{projectPath}\"");
            if (!ok)
            {
                console.MarkupLine(string.Format(messages.Failed, projectName, err));
                return false;
            }

            var slnFile = fileWriter.FindSolutionFile(solutionDir);
            if (slnFile is not null)
            {
                var folderArg = solutionFolder is not null ? $" --solution-folder \"{solutionFolder}\"" : string.Empty;
                dotNetRunner.Run($"sln \"{slnFile}\" add \"{projectCsproj}\"{folderArg}");
            }

            console.MarkupLine(string.Format(messages.Created, projectName));
        }
        else
        {
            console.MarkupLine(string.Format(messages.AlreadyExists, projectName));
        }

        AddProjectReference(projectCsproj, appCsproj, fileWriter, dotNetRunner, console);
        AddProjectReference(presentationCsproj, projectCsproj, fileWriter, dotNetRunner, console);
        foreach (var pkg in packages)
            if (!AddPackage(projectCsproj, pkg, fileWriter, dotNetRunner, console))
                return false;

        return true;
    }

    internal static void InjectAppSettingsSection(
        string presentationPath,
        string sectionKey,
        Func<JsonObject> buildSection,
        string injectedMessage,
        string warningMessage,
        IFileWriter fileWriter,
        IAnsiConsole console)
    {
        string[] candidates = ["appsettings.json", "appsettings.Development.json"];

        foreach (var fileName in candidates)
        {
            var path = Path.Combine(presentationPath, fileName);
            if (!fileWriter.FileExists(path)) continue;

            try
            {
                var json = fileWriter.ReadAllText(path);
                var root = JsonNode.Parse(json)?.AsObject();
                if (root is null || root.ContainsKey(sectionKey)) continue;

                root[sectionKey] = buildSection();
                fileWriter.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                console.MarkupLine(string.Format(injectedMessage, fileName));
            }
            catch (Exception ex)
            {
                console.MarkupLine(string.Format(warningMessage, fileName, ex.Message));
            }
        }
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
