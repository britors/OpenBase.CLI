using System.ComponentModel;
using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.Interactive;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class ScaffoldSettings : CommandSettings
{
    [CommandOption("-e|--entity <ENTIDADE>")]
    [Description("O nome da entidade a ser gerada (PascalCase, ex: Produto)")]
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

public class ScaffoldCommand(
    IAnsiConsole console,
    IProjectLocator projectLocator,
    IFileWriter fileWriter,
    IEntityPropertyCollector propertyCollector,
    IDbFlavorDetector dbFlavorDetector,
    IDotNetRunner dotNetRunner,
    IModelFirstPropertyCollector modelFirstCollector)
    : Command<ScaffoldSettings>
{
    private enum ScaffoldMode { CodeFirst, ModelFirst }

    protected override int Execute(CommandContext context, ScaffoldSettings settings, CancellationToken cancellationToken)
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

        var dbFlavor = dbFlavorDetector.Detect(solutionDir);
        var mode = AskScaffoldMode();

        var collected = CollectProperties(solutionDir, rootNamespace, dbFlavor, mode);
        if (collected is null) return 1;

        var (properties, modelFirstTableName) = collected.Value;

        if (console.Profile.Capabilities.Interactive && !console.Confirm(SR.Current.ProceedWithScaffold, defaultValue: true))
            return 0;

        var testsPath = DetectTestsPath(solutionDir, rootNamespace);
        var presentationPath = Path.Combine(solutionDir, "src", $"{rootNamespace}.Presentation.Api");
        var useJwt = fileWriter.FileExists(Path.Combine(presentationPath, "Extensions", "JwtExtensions.cs"));
        var ctx = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir)
        {
            Properties = properties,
            DbFlavor = dbFlavor,
            TestsPath = testsPath,
            TableName = modelFirstTableName,
            UseJwt = useJwt
        };

        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();
        var (created, skipped, failed) = WriteFiles(files, solutionDir, settings.Entity);
        AddTestProjectToSolution(ctx.TestsCsprojPath, solutionDir);

        var dbEditor = new DbContextEditor(fileWriter);
        var dbSetResult = dbEditor.InjectDbSet(ctx);

        PrintFileList(string.Format(SR.Current.FilesCreated, created.Count), created, "green");
        PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");

        if (failed.Count > 0)
        {
            PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");
            return 1;
        }

        if (created.Count == 0)
            return 0;

        console.MarkupLine(string.Format(SR.Current.ScaffoldSuccess, settings.Entity));

        var autoInjected = dbSetResult is DbSetInjectionResult.Injected or DbSetInjectionResult.AlreadyExists;

        if (!autoInjected)
        {
            console.MarkupLine(SR.Current.NextSteps);
            console.MarkupLine(string.Format(SR.Current.AddDbSet, settings.Entity, ctx.EPlural));
            console.MarkupLine(string.Format(SR.Current.RunMigrationsAdd, settings.Entity));
            console.MarkupLine(SR.Current.RunDatabaseUpdate);
            return 0;
        }

        var migrationRunner = new EfMigrationRunner(dotNetRunner, fileWriter, console);

        if (mode == ScaffoldMode.ModelFirst)
            migrationRunner.RunReconciliationMigration(ctx, settings.Entity);
        else
            migrationRunner.RunMigrations(ctx, settings.Entity);

        return 0;
    }


    public DbSetInjectionResult InjectDbSet(ScaffoldContext ctx) =>
        new DbContextEditor(fileWriter).InjectDbSet(ctx);

    public static string EmptyMigrationUpMethod(string content) =>
        DbContextEditor.EmptyMigrationUpMethod(content);


    private (IReadOnlyList<EntityProperty> Properties, string? TableName)? CollectProperties(
        string solutionDir, string rootNamespace, DbFlavor dbFlavor, ScaffoldMode mode)
    {
        if (mode != ScaffoldMode.ModelFirst)
            return (propertyCollector.Collect(dbFlavor), null);

        var result = modelFirstCollector.Collect(solutionDir, rootNamespace, dbFlavor);
        return result is null ? null : (result.Value.Properties, result.Value.TableName);
    }

    private ScaffoldMode AskScaffoldMode()
    {
        if (!console.Profile.Capabilities.Interactive)
            return ScaffoldMode.CodeFirst;

        var choice = console.Prompt(
            new SelectionPrompt<string>()
                .Title(SR.Current.HowToGenerateScaffold)
                .AddChoices(SR.Current.CodeFirstChoice, SR.Current.ModelFirstChoice));

        return choice == SR.Current.ModelFirstChoice ? ScaffoldMode.ModelFirst : ScaffoldMode.CodeFirst;
    }

    private string DetectTestsPath(string solutionDir, string rootNamespace)
    {
        var testsRoot = Path.Combine(solutionDir, "tests");
        var csprojName = $"{rootNamespace}.Tests.Unit.csproj";
        var found = fileWriter.FindFile(testsRoot, csprojName);
        return found is not null
            ? Path.GetDirectoryName(found)!
            : Path.Combine(testsRoot, $"{rootNamespace}.Tests.Unit");
    }

    private void AddTestProjectToSolution(string testCsprojPath, string solutionDir)
    {
        var slnFile = fileWriter.FindSolutionFile(solutionDir);
        if (slnFile is null || !fileWriter.FileExists(testCsprojPath))
            return;

        var (success, error) = dotNetRunner.Run($"sln \"{slnFile}\" add \"{testCsprojPath}\"");

        if (!success && !string.IsNullOrWhiteSpace(error))
            console.MarkupLine($"[yellow]Aviso:[/] Não foi possível adicionar o projeto de testes à solution: {Markup.Escape(error)}");
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
