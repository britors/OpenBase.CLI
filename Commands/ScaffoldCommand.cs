using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers;
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
    IDotNetRunner dotNetRunner)
    : Command<ScaffoldSettings>
{
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
        var properties = propertyCollector.Collect(dbFlavor);

        if (console.Profile.Capabilities.Interactive && !console.Confirm("Prosseguir com o scaffold?", defaultValue: true))
            return 0;

        var testsPath = DetectTestsPath(solutionDir, rootNamespace);
        var ctx = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir)
        {
            Properties = properties,
            DbFlavor = dbFlavor,
            TestsPath = testsPath
        };

        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();
        var (created, skipped, failed) = WriteFiles(files, solutionDir, settings.Entity);
        AddTestFilesToCsproj(ctx, created, solutionDir);
        AddTestProjectToSolution(ctx.TestsCsprojPath, solutionDir);

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
        console.MarkupLine("Próximos passos:");
        console.MarkupLine($"  1. Adicione [blue]DbSet<{settings.Entity}> {settings.Entity}s {{ get; set; }}[/] ao DbContext");
        console.MarkupLine($"  2. Execute [blue]dotnet ef migrations add Add{settings.Entity}[/]");
        console.MarkupLine("  3. Execute [blue]dotnet ef database update[/]");

        return 0;
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

    private void AddTestFilesToCsproj(ScaffoldContext ctx, List<string> createdRelPaths, string solutionDir)
    {
        if (!fileWriter.FileExists(ctx.TestsCsprojPath))
            return;

        var testsRelDir = Path.GetRelativePath(solutionDir, ctx.TestsPath);
        var prefix = testsRelDir + Path.DirectorySeparatorChar;

        var testFiles = createdRelPaths
            .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(f => f[prefix.Length..])
            .ToList();

        if (testFiles.Count == 0)
            return;

        var content = fileWriter.ReadAllText(ctx.TestsCsprojPath);
        if (string.IsNullOrEmpty(content))
            return;

        var toAdd = testFiles
            .Where(f => !content.Contains(Path.GetFileName(f), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toAdd.Count == 0)
            return;

        const string closeTag = "</Project>";
        var idx = content.LastIndexOf(closeTag, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return;

        var lines = toAdd.Select(f => $"    <Compile Include=\"{f}\" />");
        var itemGroup = $"  <ItemGroup>\n{string.Join("\n", lines)}\n  </ItemGroup>\n";
        fileWriter.WriteAllText(ctx.TestsCsprojPath, content[..idx] + itemGroup + content[idx..]);
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
