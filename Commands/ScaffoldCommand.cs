using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
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
            return ValidationResult.Error("O parâmetro --entity <ENTIDADE> é obrigatório.");

        if (!char.IsUpper(Entity[0]))
            return ValidationResult.Error("O nome da entidade deve começar com letra maiúscula (PascalCase).");

        if (!Entity.All(char.IsLetterOrDigit))
            return ValidationResult.Error("O nome da entidade deve conter apenas letras e números.");

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
            console.MarkupLine("[red]Erro:[/] Estrutura de projeto OpenBase não encontrada.");
            console.MarkupLine("Execute este comando na raiz de um projeto criado com [blue]openbase new[/].");
            console.MarkupLine("Ou informe o namespace com [blue]--namespace <NAMESPACE>[/].");
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

        if (console.Profile.Capabilities.Interactive && !console.Confirm("Prosseguir com o scaffold?", defaultValue: true))
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

        PrintFileList($"{created.Count} arquivo(s) criado(s):", created, "green");
        PrintFileList($"{skipped.Count} arquivo(s) já existente(s) ignorado(s):", skipped, "yellow");

        if (failed.Count > 0)
        {
            PrintFileList($"{failed.Count} erro(s):", failed, "red", "red");
            return 1;
        }

        if (created.Count == 0)
            return 0;

        console.MarkupLine($"\n[green]Scaffold da entidade [bold]{settings.Entity}[/] gerado com sucesso![/]");

        var autoInjected = dbSetResult is DbSetInjectionResult.Injected or DbSetInjectionResult.AlreadyExists;

        if (!autoInjected)
        {
            console.MarkupLine("Próximos passos:");
            console.MarkupLine($"  1. Adicione [blue]DbSet<{settings.Entity}> {ctx.EPlural} {{ get; set; }}[/] ao DbContext");
            if (!skipMigration)
            {
                console.MarkupLine($"  2. Execute [blue]dotnet ef migrations add Add{settings.Entity}[/]");
                console.MarkupLine("  3. Execute [blue]dotnet ef database update[/]");
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
                .Title("\nComo deseja gerar o scaffold?")
                .AddChoices(
                    "Code First (definir propriedades manualmente)",
                    "Model First (ler estrutura de uma tabela existente)"));

        return choice.StartsWith("Model") ? ScaffoldMode.ModelFirst : ScaffoldMode.CodeFirst;
    }


    private void RunMigrations(ScaffoldContext ctx, string entity)
    {
        (bool Success, string Error)? restoreResult = null;
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Restaurando pacotes NuGet...", _ =>
            {
                restoreResult = dotNetRunner.Run($"restore \"{ctx.InfraContextPath}\"");
            });

        if (restoreResult is { Success: false })
        {
            console.MarkupLine("[yellow]Aviso:[/] Falha ao restaurar pacotes. Tentando gerar a migration mesmo assim...");
            if (!string.IsNullOrWhiteSpace(restoreResult.Value.Error))
                console.MarkupLine($"[grey]{Markup.Escape(restoreResult.Value.Error)}[/]");
        }

        var (migrationOk, migrationError) = RunEfCommand(
            $"migrations add Add{entity}",
            $"Gerando migration [blue]Add{entity}[/]...",
            ctx);

        if (!migrationOk)
        {
            console.MarkupLine("[red]Erro:[/] Falha ao gerar a migration.");
            if (!string.IsNullOrWhiteSpace(migrationError))
                console.MarkupLine($"[grey]{Markup.Escape(migrationError)}[/]");
            console.MarkupLine($"Execute manualmente: [blue]dotnet ef migrations add Add{entity}[/]");
            return;
        }

        console.MarkupLine($"[green]Migration Add{entity} gerada.[/]");

        if (!console.Profile.Capabilities.Interactive ||
            !console.Confirm("Executar [blue]database update[/] agora?", defaultValue: true))
            return;

        var (updateOk, updateError) = RunEfCommand(
            "database update",
            "Executando [blue]database update[/]...",
            ctx);

        if (!updateOk)
        {
            console.MarkupLine("[red]Erro:[/] Falha ao executar database update.");
            if (!string.IsNullOrWhiteSpace(updateError))
                console.MarkupLine($"[grey]{Markup.Escape(updateError)}[/]");
            console.MarkupLine("[blue]dotnet ef database update[/]");
            return;
        }

        console.MarkupLine("[green]Banco de dados atualizado com sucesso.[/]");
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
            .Start($"Gerando scaffold para [blue]{entityName}[/]...", _ =>
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
