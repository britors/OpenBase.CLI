using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class NewSettings : CommandSettings
{
    [CommandOption("-t|--type <TIPO>")]
    [Description("O tipo do template [api]")]
    [DefaultValue("api")]
    public string Type { get; set; } = "api";

    [CommandOption("-s|--template <TEMPLATE>")]
    [Description("O nome do template [sqlserver|pgsql]")]
    public string TemplateName { get; set; } = "sqlserver";

    [CommandOption("-n|--name <NOME>")]
    [Description("O nome do projeto a ser criado")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
            return ValidationResult.Error("O parâmetro --template <TEMPLATE> é obrigatório.");

        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("O parâmetro --name <NOME> é obrigatório.");

        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat([' ', '&', '|', ';', '`', '$', '(', ')'])
            .ToArray();

        return Name.IndexOfAny(invalidChars) >= 0 ? ValidationResult.Error("O nome do projeto contém caracteres inválidos. Use apenas letras, números, '-' e '_'.") : ValidationResult.Success();
    }
}

public class NewCommand : AsyncCommand<NewSettings>
{
    private static readonly Dictionary<string, string> TemplateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "api:sqlserver", "openbasenet-sql" },
        { "api:pgsql", "openbasenet-pgsql" },
    };

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] NewSettings settings,
        CancellationToken cancellationToken)
    {
        var key = $"{settings.Type}:{settings.TemplateName}";

        if (!TemplateMap.TryGetValue(key, out var shortName))
        {
            AnsiConsole.MarkupLine(
                $"[red]Erro:[/] A combinação Tipo '[yellow]{settings.Type}[/]' + Template '[yellow]{settings.TemplateName}[/]' não é válida.");
            AnsiConsole.MarkupLine("Combinações disponíveis: [blue]--type api --template sqlserver[/]");
            return 1;
        }

        var exitCode = 0;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Criando projeto [blue]{settings.Name}[/]...", async _ =>
            {
                var psi = new ProcessStartInfo(
                    Helpers.DotNet.GetDotnetPath(),
                    $"new {shortName} -n {settings.Name} -o {settings.Name}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var errorOutput = process.StandardError.ReadToEndAsync(cancellationToken);
                    await process.WaitForExitAsync(cancellationToken);
                    if (process.ExitCode != 0)
                    {
                        exitCode = 1;
                        var error = await errorOutput;
                        AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao criar o projeto. Verifique se o template está instalado com [blue]openbase install[/].");
                        if (!string.IsNullOrWhiteSpace(error))
                            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(error.Trim())}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[grey]  cd {settings.Name}[/]");
                        AnsiConsole.MarkupLine("[grey]  dotnet run --project src/...[/]");
                    }
                }
            });

        return exitCode;
    }
}
