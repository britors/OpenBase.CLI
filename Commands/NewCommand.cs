using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers;
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

        return Name.IndexOfAny(invalidChars) >= 0
            ? ValidationResult.Error("O nome do projeto contém caracteres inválidos. Use apenas letras, números, '-' e '_'.")
            : ValidationResult.Success();
    }
}

public class NewCommand : AsyncCommand<NewSettings>
{
    private static readonly Dictionary<string, IDbTemplateStrategy> TemplateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "api:sqlserver", new SqlServerTemplateStrategy() },
        { "api:pgsql",     new PostgresTemplateStrategy() },
    };

    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;
    private readonly IProjectConfigurator _configurator;
    private readonly IFileWriter _fileWriter;

    public NewCommand(IDotNetRunner dotNetRunner, IAnsiConsole console, IProjectConfigurator configurator, IFileWriter fileWriter)
    {
        _dotNetRunner = dotNetRunner;
        _console = console;
        _configurator = configurator;
        _fileWriter = fileWriter;
    }

    private const int RequiredSdkMajorVersion = 10;

    protected override async Task<int> ExecuteAsync(
        [NotNull] CommandContext context,
        [NotNull] NewSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_dotNetRunner.IsSdkVersionSufficient(RequiredSdkMajorVersion))
        {
            _console.MarkupLine($"[red]Erro:[/] O .NET SDK instalado é incompatível com esta versão do OpenBase.");
            _console.MarkupLine($"É necessário o [blue].NET {RequiredSdkMajorVersion}[/] ou superior. Atualize o SDK em: [blue]https://dot.net[/]");
            return 1;
        }

        var key = $"{settings.Type}:{settings.TemplateName}";

        if (!TemplateMap.TryGetValue(key, out var strategy))
        {
            _console.MarkupLine(
                $"[red]Erro:[/] A combinação Tipo '[yellow]{settings.Type}[/]' + Template '[yellow]{settings.TemplateName}[/]' não é válida.");
            _console.MarkupLine("Combinações disponíveis: [blue]--type api --template sqlserver[/]");
            return 1;
        }

        var config = _configurator.Collect(strategy);

        var exitCode = 0;

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Criando projeto [blue]{settings.Name}[/]...", async _ =>
            {
                var (success, error) = await _dotNetRunner.RunAsync(
                    $"new {strategy.ShortName} -n {settings.Name} -o {settings.Name}", cancellationToken);

                if (!success)
                {
                    exitCode = 1;
                    _console.MarkupLine("[red]Erro:[/] Falha ao criar o projeto. Verifique se o template está instalado com [blue]openbase install[/].");
                    if (!string.IsNullOrWhiteSpace(error))
                        _console.MarkupLine($"[grey]{Markup.Escape(error)}[/]");
                }
                else
                {
                    UpdateAppSettings(settings.Name, strategy, config, _fileWriter);
                    _console.MarkupLine($"[grey]  cd {settings.Name}[/]");
                    _console.MarkupLine("[grey]  dotnet run --project src/...[/]");
                }
            });

        return exitCode;
    }

    public static void UpdateAppSettings(string projectName, IDbTemplateStrategy strategy, ProjectSetupConfig config, IFileWriter fileWriter)
    {
        var basePath = Path.Combine(projectName, "src", $"{projectName}.Presentation.Api");
        var connectionString = strategy.BuildConnectionString(projectName, config.DbServer, config.DbUser, config.DbPassword);

        foreach (var fileName in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var path = Path.Combine(basePath, fileName);
            if (!fileWriter.FileExists(path)) continue;

            var updated = ApplyConfigToJson(fileWriter.ReadAllText(path), strategy.ConnectionKey, connectionString, config);
            fileWriter.WriteAllText(path, updated);
        }
    }

    public static string ApplyConfigToJson(string jsonContent, string connectionKey, string connectionString, ProjectSetupConfig config)
    {
        JsonNode? json;
        try { json = JsonNode.Parse(jsonContent); }
        catch (JsonException) { return jsonContent; }
        if (json is null) return jsonContent;

        if (json["ConnectionStrings"] is JsonObject connStrings)
            connStrings[connectionKey] = connectionString;

        foreach (var key in new[] { "Mediatr", "Mediator" })
            if (json[key] is JsonObject node)
                node["LicenseKey"] = config.MediatrLicense;

        foreach (var key in new[] { "Automapper", "AutoMapper" })
            if (json[key] is JsonObject node)
                node["LicenseKey"] = config.AutomapperLicense;

        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
