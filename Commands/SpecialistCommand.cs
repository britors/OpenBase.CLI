using System.ComponentModel;
using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class SpecialistSettings : CommandSettings
{
    [CommandOption("-e|--entity <ENTIDADE>")]
    [Description("O nome da entidade (PascalCase, ex: Produto)")]
    public string Entity { get; set; } = string.Empty;

    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Namespace raiz do projeto (detectado automaticamente se omitido)")]
    public string? RootNamespace { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Entity))
            return ValidationResult.Error(SR.Current.EntityParamRequired);

        if (!char.IsUpper(Entity[0]))
            return ValidationResult.Error(SR.Current.EntityMustBePascalCase);

        if (!Entity.All(char.IsLetterOrDigit))
            return ValidationResult.Error(SR.Current.EntityMustBeAlphanumeric);

        return ValidationResult.Success();
    }
}

public class SpecialistCommand(
    IAnsiConsole console,
    IProjectLocator projectLocator,
    IFileWriter fileWriter)
    : Command<SpecialistSettings>
{
    protected override int Execute(CommandContext context, SpecialistSettings settings, CancellationToken cancellationToken)
    {
        var (solutionDir, rootNamespace) = projectLocator.Detect(
            Directory.GetCurrentDirectory(), settings.RootNamespace);

        if (solutionDir is null || rootNamespace is null)
        {
            console.MarkupLine(SR.Current.ProjectStructureNotFound);
            console.MarkupLine(SR.Current.RunInProjectRoot);
            console.MarkupLine(SR.Current.OrProvideNamespace);
            return 1;
        }

        var ctx       = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir);
        var generator = new ScaffoldGenerator(ctx);
        var wizard    = new SpecialistWizard(console);

        do
        {
            var definition = wizard.AskDefinition();
            if (definition is null) return 0;

            var files = generator.GetSpecialistFiles(definition).ToList();
            var (created, skipped, failed) = WriteFiles(files, solutionDir, settings.Entity);

            PrintFileList(string.Format(SR.Current.SpecialistFilesCreated, created.Count), created, "green");
            PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");
            if (failed.Count > 0)
                PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");

        } while (console.Confirm(SR.Current.SpecialistAddAnother, defaultValue: false));

        return 0;
    }

    private (List<string> Created, List<string> Skipped, List<string> Failed) WriteFiles(
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

    private void PrintFileList(string header, List<string> files, string headerColor, string fileColor = "grey")
    {
        if (files.Count == 0) return;
        console.MarkupLine($"\n[{headerColor}]{header}[/]");
        foreach (var f in files)
            console.MarkupLine($"  [{fileColor}]{Markup.Escape(f)}[/]");
    }
}
