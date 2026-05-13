using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
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
    private const string DbContextFileName = "OneBaseDataBaseContext.cs";

    public enum DbSetInjectionResult { Injected, AlreadyExists, FileNotFound, Failed }

    private enum ScaffoldMode { CodeFirst, ModelFirst }

    protected override int Execute([NotNull] CommandContext context, [NotNull] ScaffoldSettings settings, CancellationToken cancellationToken)
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

        IReadOnlyList<EntityProperty> properties;
        bool skipMigration;

        string? modelFirstTableName = null;

        if (mode == ScaffoldMode.ModelFirst)
        {
            var result = modelFirstCollector.Collect(solutionDir, rootNamespace, dbFlavor);
            if (result is null) return 1;
            properties = result.Value.Properties;
            modelFirstTableName = result.Value.TableName;
            skipMigration = true;
        }
        else
        {
            properties = propertyCollector.Collect(dbFlavor);
            skipMigration = false;
        }

        if (console.Profile.Capabilities.Interactive && !console.Confirm(SR.Current.ProceedWithScaffold, defaultValue: true))
            return 0;

        var testsPath = DetectTestsPath(solutionDir, rootNamespace);
        var ctx = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir)
        {
            Properties = properties,
            DbFlavor = dbFlavor,
            TestsPath = testsPath,
            TableName = modelFirstTableName
        };

        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();
        var (created, skipped, failed) = WriteFiles(files, solutionDir, settings.Entity);
        AddTestProjectToSolution(ctx.TestsCsprojPath, solutionDir);
        var dbSetResult = InjectDbSet(ctx);

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
            if (!skipMigration)
            {
                console.MarkupLine(string.Format(SR.Current.RunMigrationsAdd, settings.Entity));
                console.MarkupLine(SR.Current.RunDatabaseUpdate);
            }
            return 0;
        }

        if (!skipMigration)
            RunMigrations(ctx, settings.Entity);

        return 0;
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


    private void RunMigrations(ScaffoldContext ctx, string entity)
    {
        (bool Success, string Error)? restoreResult = null;
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(SR.Current.RestoringNuGetPackages, _ =>
            {
                restoreResult = dotNetRunner.Run($"restore \"{ctx.InfraContextPath}\"");
            });

        if (restoreResult is { Success: false })
        {
            console.MarkupLine(SR.Current.RestorePackagesWarning);
            if (!string.IsNullOrWhiteSpace(restoreResult.Value.Error))
                console.MarkupLine($"[grey]{Markup.Escape(restoreResult.Value.Error)}[/]");
        }

        var (migrationOk, migrationError) = RunEfCommand(
            $"migrations add Add{entity}",
            string.Format(SR.Current.GeneratingMigration, entity),
            ctx);

        if (!migrationOk)
        {
            console.MarkupLine(SR.Current.MigrationFailed);
            if (!string.IsNullOrWhiteSpace(migrationError))
                console.MarkupLine($"[grey]{Markup.Escape(migrationError)}[/]");
            console.MarkupLine(string.Format(SR.Current.RunMigrationManually, entity));
            return;
        }

        console.MarkupLine(string.Format(SR.Current.MigrationGenerated, entity));

        if (!console.Profile.Capabilities.Interactive ||
            !console.Confirm(SR.Current.RunDatabaseUpdateNow, defaultValue: true))
            return;

        var (updateOk, updateError) = RunEfCommand(
            "database update",
            SR.Current.ExecutingDatabaseUpdate,
            ctx);

        if (!updateOk)
        {
            console.MarkupLine(SR.Current.DatabaseUpdateFailed);
            if (!string.IsNullOrWhiteSpace(updateError))
                console.MarkupLine($"[grey]{Markup.Escape(updateError)}[/]");
            console.MarkupLine(SR.Current.DotnetEfDatabaseUpdate);
            return;
        }

        console.MarkupLine(SR.Current.DatabaseUpdatedSuccess);
    }

    private (bool Success, string Error) RunEfCommand(string efArgs, string spinnerLabel, ScaffoldContext ctx)
    {
        var projectArg = $"--project \"{ctx.InfraContextPath}\" --startup-project \"{ctx.PresentationPath}\"";
        (bool Success, string Error)? result = null;

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(spinnerLabel, _ =>
            {
                result = dotNetRunner.Run($"ef {efArgs} {projectArg}");
            });

        return result ?? (false, string.Empty);
    }

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

            var sep = content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();

            var entitiesUsing = $"using {ctx.NS}.Domain.Entities;";
            if (!content.Contains(entitiesUsing))
            {
                var lastUsing = lines.FindLastIndex(l => l.TrimStart().StartsWith("using "));
                if (lastUsing >= 0)
                    lines.Insert(lastUsing + 1, entitiesUsing);
                else
                    lines.Insert(0, entitiesUsing);
            }

            var dbSetLine = $"    public DbSet<{ctx.Entity}> {ctx.EPlural} {{ get; set; }}";

            var lastDbSet = lines.FindLastIndex(l => l.Contains("DbSet<"));
            if (lastDbSet >= 0)
            {
                lines.Insert(lastDbSet + 1, dbSetLine);
            }
            else
            {
                var classIdx = lines.FindIndex(l => l.Contains("class OneBaseDataBaseContext"));
                if (classIdx < 0) return DbSetInjectionResult.Failed;

                var braceIdx = lines.FindIndex(classIdx, l => l.Trim() == "{");
                if (braceIdx < 0) return DbSetInjectionResult.Failed;

                lines.Insert(braceIdx + 1, dbSetLine);
                lines.Insert(braceIdx + 2, string.Empty);
            }

            fileWriter.WriteAllText(path, string.Join(sep, lines));
            return DbSetInjectionResult.Injected;
        }
        catch
        {
            return DbSetInjectionResult.Failed;
        }
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
        var failed = new List<string>();

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
