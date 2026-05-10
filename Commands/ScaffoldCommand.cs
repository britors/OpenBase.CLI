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

public class ScaffoldCommand(IAnsiConsole console, IProjectLocator projectLocator, IFileWriter fileWriter)
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

        var ctx = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir);
        var generator = new ScaffoldGenerator(ctx);
        var files = generator.GetFiles().ToList();

        var created = new List<string>();
        var skipped = new List<string>();
        var failed = new List<string>();

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Gerando scaffold para [blue]{settings.Entity}[/]...", _ =>
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

        if (created.Count > 0)
        {
            console.MarkupLine($"\n[green]{created.Count} arquivo(s) criado(s):[/]");
            foreach (var f in created)
                console.MarkupLine($"  [grey]{Markup.Escape(f)}[/]");
        }

        if (skipped.Count > 0)
        {
            console.MarkupLine($"\n[yellow]{skipped.Count} arquivo(s) já existente(s) ignorado(s):[/]");
            foreach (var f in skipped)
                console.MarkupLine($"  [grey]{Markup.Escape(f)}[/]");
        }

        if (failed.Count > 0)
        {
            console.MarkupLine($"\n[red]{failed.Count} erro(s):[/]");
            foreach (var f in failed)
                console.MarkupLine($"  [red]{Markup.Escape(f)}[/]");
            return 1;
        }

        if (created.Count > 0)
        {
            console.MarkupLine($"\n[green]Scaffold da entidade [bold]{settings.Entity}[/] gerado com sucesso![/]");
            console.MarkupLine("Próximos passos:");
            console.MarkupLine($"  1. Adicione [blue]DbSet<{settings.Entity}> {settings.Entity}s {{ get; set; }}[/] ao DbContext");
            console.MarkupLine($"  2. Execute [blue]dotnet ef migrations add Add{settings.Entity}[/]");
            console.MarkupLine("  3. Execute [blue]dotnet ef database update[/]");
        }

        return 0;
    }
}
