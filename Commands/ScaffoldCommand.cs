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

    [CommandOption("-u|--update")]
    [Description("Atualiza os arquivos gerados com base na estrutura atual da tabela no banco de dados")]
    public bool Update { get; set; }

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

        if (settings.Update)
            return ExecuteUpdate(settings, solutionDir, rootNamespace, dbFlavor);

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

        var generator = new ScaffoldGenerator(ctx);
        var files = generator.GetFiles().ToList();
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

        AskAndGenerateSpecialists(generator, solutionDir, settings.Entity);

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

    private void AskAndGenerateSpecialists(ScaffoldGenerator generator, string solutionDir, string entityName)
    {
        if (!console.Profile.Capabilities.Interactive) return;
        if (!console.Confirm(SR.Current.SpecialistAddPrompt, defaultValue: false)) return;

        var wizard = new SpecialistWizard(console);

        do
        {
            var definition = wizard.AskDefinition();
            if (definition is null) return;

            var files = generator.GetSpecialistFiles(definition).ToList();
            var (created, skipped, failed) = WriteFiles(files, solutionDir, entityName);

            PrintFileList(string.Format(SR.Current.SpecialistFilesCreated, created.Count), created, "green");
            PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");
            if (failed.Count > 0)
                PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");

        } while (console.Confirm(SR.Current.SpecialistAddAnother, defaultValue: false));
    }

    private int ExecuteUpdate(ScaffoldSettings settings, string solutionDir, string rootNamespace, DbFlavor dbFlavor)
    {
        var entityFilePath = Path.Combine(solutionDir, "src", $"{rootNamespace}.Domain", "Entities", $"{settings.Entity}.cs");

        if (!fileWriter.FileExists(entityFilePath))
        {
            console.MarkupLine(string.Format(SR.Current.ScaffoldUpdateEntityNotFound, settings.Entity));
            return 1;
        }

        var oldProperties = ScaffoldPropertyParser.Parse(fileWriter.ReadAllText(entityFilePath));

        var result = modelFirstCollector.Collect(solutionDir, rootNamespace, dbFlavor);
        if (result is null) return 1;

        var (newProperties, tableName) = result.Value;
        var diff = ScaffoldDiff.Compute(oldProperties, newProperties);

        if (!diff.HasChanges)
        {
            console.MarkupLine(SR.Current.ScaffoldUpdateNoChanges);
            return 0;
        }

        PrintDiff(diff);
        WarnIfUncommittedChanges(solutionDir, settings.Entity);

        if (console.Profile.Capabilities.Interactive && !console.Confirm(SR.Current.ScaffoldUpdateApplyChanges, defaultValue: true))
            return 0;

        if (diff.Removed.Count > 0 && console.Profile.Capabilities.Interactive)
        {
            console.MarkupLine(string.Format(SR.Current.ScaffoldUpdateDestructiveWarn, diff.Removed.Count));
            if (!console.Confirm(SR.Current.ScaffoldUpdateConfirmRemoval, defaultValue: false))
                return 0;
        }

        var testsPath = DetectTestsPath(solutionDir, rootNamespace);
        var presentationPath = Path.Combine(solutionDir, "src", $"{rootNamespace}.Presentation.Api");
        var useJwt = fileWriter.FileExists(Path.Combine(presentationPath, "Extensions", "JwtExtensions.cs"));

        var ctx = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir)
        {
            Properties = newProperties,
            DbFlavor = dbFlavor,
            TestsPath = testsPath,
            TableName = tableName,
            UseJwt = useJwt
        };

        var files = new ScaffoldGenerator(ctx).GetPropertyDependentFiles().ToList();
        var (updated, failed) = OverwriteFiles(files, solutionDir, settings.Entity);

        PrintFileList(string.Format(SR.Current.FilesUpdated, updated.Count), updated, "green");

        if (failed.Count > 0)
        {
            PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");
            return 1;
        }

        console.MarkupLine(string.Format(SR.Current.ScaffoldUpdateSuccess, settings.Entity));

        new EfMigrationRunner(dotNetRunner, fileWriter, console)
            .RunUpdateMigration(ctx, settings.Entity);

        return 0;
    }

    private (List<string> Updated, List<string> Failed) OverwriteFiles(
        IEnumerable<(string Path, string Content)> files,
        string solutionDir,
        string entityName)
    {
        var updated = new List<string>();
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
                        fileWriter.WriteAllText(path, content);
                        updated.Add(rel);
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{rel}: {ex.Message}");
                    }
                }
            });

        return (updated, failed);
    }

    private void PrintDiff(ScaffoldDiff diff)
    {
        console.MarkupLine(SR.Current.ScaffoldUpdateDiffTitle);

        foreach (var p in diff.Added)
            console.MarkupLine($"  [green]+[/] {Markup.Escape(p.Name)} ({Markup.Escape(p.ActualCsType)})  [grey]→ nova coluna[/]");

        foreach (var p in diff.Removed)
            console.MarkupLine($"  [red]-[/] {Markup.Escape(p.Name)} ({Markup.Escape(p.ActualCsType)})  [grey]→ coluna removida[/]");

        foreach (var (old, neo) in diff.Changed)
        {
            var oldDesc = $"{old.ActualCsType}";
            var newDesc = $"{neo.ActualCsType}";
            console.MarkupLine($"  [yellow]~[/] {Markup.Escape(old.Name)}: {Markup.Escape(oldDesc)} → {Markup.Escape(newDesc)}  [grey]→ tipo alterado[/]");
        }
    }

    private void WarnIfUncommittedChanges(string solutionDir, string entityName)
    {
        try
        {
            var gitPath = ResolveExecutable("git");
            if (gitPath is null) return;

            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo(gitPath, "status --short")
            {
                WorkingDirectory = solutionDir,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            var entityFiles = output.Split('\n')
                .Where(l => l.Contains(entityName, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            if (entityFiles.Count == 0) return;

            console.MarkupLine(SR.Current.ScaffoldUpdateUncommittedWarn);
            foreach (var f in entityFiles)
                console.MarkupLine($"  [yellow]{Markup.Escape(f)}[/]");
            console.MarkupLine(SR.Current.ScaffoldUpdateUncommittedHint);
        }
        catch { /* git not available, silently ignore */ }
    }

    private static string? ResolveExecutable(string name)
    {
        var fileName = OperatingSystem.IsWindows() ? name + ".exe" : name;
        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator)
            .Select(dir => Path.Combine(dir, fileName))
            .FirstOrDefault(File.Exists);
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
