using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace OpenBaseNetSqlServerCLI.Commands;

public class NewSettings : CommandSettings
{
    [CommandOption("-t|--type <TIPO>")]
    [Description("O tipo do template (ex: api)")]
    [DefaultValue("api")]
    public string Type { get; set; } = "api";

    // Mudei o atalho para -s (de stack ou sql) ou -p (package) para não conflitar com -t de type
    [CommandOption("-s|--template <TEMPLATE>")]
    [Description("O nome do template (ex: sqlserver)")]
    public string TemplateName { get; set; } = string.Empty;

    // Ajustado para -n|--name para que o comando aceite --name <NOME>
    [CommandOption("-n|--name <NOME>")]
    [Description("O nome do projeto a ser criado")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
            return ValidationResult.Error("O parâmetro --template <TEMPLATE> é obrigatório.");

        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error("O parâmetro --name <NOME> é obrigatório.");

        // ValidationResult.Success é uma propriedade estática, não um método.
        return ValidationResult.Success();
    }
}

public class NewCommand : AsyncCommand<NewSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] NewSettings settings, CancellationToken cancellationToken)
    {
        // 1. Mapeamento de combinações para os Short Names dos templates instalados
        var templateMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "api:sqlserver", "openbasenet-sql" }
            // Espaço para novos mapeamentos: { "api:mongodb", "openbasenet-mongo" }
        };

        var key = $"{settings.Type}:{settings.TemplateName}";

        if (!templateMap.TryGetValue(key, out var shortName))
        {
            AnsiConsole.MarkupLine($"[red]Erro:[/] A combinação de Tipo '[yellow]{settings.Type}[/]' e Template '[yellow]{settings.TemplateName}[/]' não é válida.");
            return 1;
        }

        // 2. Execução do comando de criação
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Criando projeto [blue]{settings.Name}[/]...", async ctx =>
            {
                // dotnet new <shortname> -n <nome> -o <nome>

                var psi = new ProcessStartInfo(Helpers.DotNet.GetDotnetPath(), $"new {shortName} -n {settings.Name} -o {settings.Name}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };



                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync(cancellationToken);

                    if (process.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]Erro:[/] Falha ao executar 'dotnet new'. Verifique se o template está instalado.");
                    }
                }
            });

        AnsiConsole.MarkupLine($"[green]Sucesso:[/] Projeto [blue]{settings.Name}[/] criado com sucesso!");

        return 0;
    }
}