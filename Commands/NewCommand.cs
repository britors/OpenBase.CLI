using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
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

public class NewSettings : CommandSettings
{
    [CommandOption("-t|--type <TIPO>")]
    [Description("O tipo do template [api]")]
    [DefaultValue("api")]
    public string Type { get; set; } = "api";

    [CommandOption("-s|--template <TEMPLATE>")]
    [Description("O nome do template [sqlserver|pgsql|oracle]")]
    public string? TemplateName { get; set; }

    [CommandOption("-n|--name <NOME>")]
    [Description("O nome do projeto a ser criado")]
    public string Name { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return ValidationResult.Error(SR.Current.NameParamRequired);

        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat([' ', '&', '|', ';', '`', '$', '(', ')'])
            .ToArray();

        return Name.IndexOfAny(invalidChars) >= 0
            ? ValidationResult.Error(SR.Current.ProjectNameInvalid)
            : ValidationResult.Success();
    }
}

public class NewCommand : AsyncCommand<NewSettings>
{
    private static readonly Dictionary<string, IDbTemplateStrategy> TemplateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "api:sqlserver", new SqlServerTemplateStrategy() },
        { "api:pgsql",     new PostgresTemplateStrategy()  },
        { "api:oracle",    new OracleTemplateStrategy()    },
    };

    private const string JsonSectionConnectionStrings = "ConnectionStrings";
    private const string JsonKeyLicenseKey            = "LicenseKey";
    private const string ApiSourceDir                 = "src";
    private const string ApiProjectDir               = "OpenBaseNET.Presentation.Api";

    private static readonly string[] AppSettingsFiles = ["appsettings.json", "appsettings.Development.json"];
    private static readonly string[] MediatRKeys      = ["Mediatr", "Mediator"];
    private static readonly string[] AutoMapperKeys   = ["Automapper", "AutoMapper"];

    private readonly IDotNetRunner _dotNetRunner;
    private readonly IAnsiConsole _console;
    private readonly IProjectConfigurator _configurator;
    private readonly IFileWriter _fileWriter;
    private readonly IDbSchemaReader _dbSchemaReader;

    public NewCommand(IDotNetRunner dotNetRunner, IAnsiConsole console, IProjectConfigurator configurator, IFileWriter fileWriter, IDbSchemaReader dbSchemaReader)
    {
        _dotNetRunner = dotNetRunner;
        _console = console;
        _configurator = configurator;
        _fileWriter = fileWriter;
        _dbSchemaReader = dbSchemaReader;
    }

    private const int RequiredSdkMajorVersion = 10;

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        NewSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_dotNetRunner.IsSdkVersionSufficient(RequiredSdkMajorVersion))
        {
            _console.MarkupLine(SR.Current.SdkIncompatible);
            _console.MarkupLine(string.Format(SR.Current.SdkUpdateRequired, RequiredSdkMajorVersion));
            return 1;
        }

        var templateName = settings.TemplateName;
        if (string.IsNullOrWhiteSpace(templateName))
        {
            templateName = await _console.PromptAsync(
                new SelectionPrompt<string>()
                    .Title(SR.Current.ApiDatabasePrompt)
                    .AddChoices("sqlserver", "pgsql", "oracle"), cancellationToken);
        }

        var key = $"{settings.Type}:{templateName}";

        if (!TemplateMap.TryGetValue(key, out var strategy))
        {
            _console.MarkupLine(string.Format(SR.Current.InvalidTypeCombination, settings.Type, settings.TemplateName));
            _console.MarkupLine(SR.Current.AvailableCombinations);
            return 1;
        }

        var config = _configurator.Collect(strategy, settings.Name);

        var exitCode = 0;

        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(string.Format(SR.Current.CreatingProject, settings.Name), async _ =>
            {
                var (success, error) = await _dotNetRunner.RunAsync(
                    $"new {strategy.ShortName} -n {settings.Name} -o {settings.Name}", cancellationToken);

                if (!success)
                {
                    exitCode = 1;
                    _console.MarkupLine(SR.Current.CreateProjectFailed);
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

        if (exitCode == 0)
        {
            var connectionString = strategy.BuildConnectionString(config.DbName, config.DbServer, config.DbUser, config.DbPassword);
            await RunBulkImportAsync(settings.Name, connectionString, strategy.DbFlavor, cancellationToken);
        }

        return exitCode;
    }

    private async Task RunBulkImportAsync(
        string projectName,
        string connectionString,
        DbFlavor dbFlavor,
        CancellationToken cancellationToken)
    {
        if (!_console.Profile.Capabilities.Interactive) return;

        var connected = false;
        _console.Status().Spinner(Spinner.Known.Dots)
            .Start(SR.Current.TestingDbConnection, _ =>
                connected = _dbSchemaReader.TryConnect(connectionString, dbFlavor));

        if (!connected)
        {
            _console.MarkupLine(SR.Current.DbConnectionFailed);
            return;
        }

        _console.MarkupLine(SR.Current.DbConnectionSuccess);

        if (!_console.Confirm(SR.Current.ImportFullModelPrompt, defaultValue: false))
            return;

        IReadOnlyList<DbTableInfo> tables = [];
        _console.Status().Spinner(Spinner.Known.Dots)
            .Start(SR.Current.ListingTables, _ =>
                tables = _dbSchemaReader.ListTables(connectionString, dbFlavor));

        if (tables.Count == 0)
        {
            _console.MarkupLine(SR.Current.NoTablesFound);
            return;
        }

        _console.MarkupLine(string.Format(SR.Current.TablesFound, tables.Count));
        _console.WriteLine();

        var toScaffold = new List<(DbTableInfo Table, string EntityName)>();
        foreach (var table in tables)
        {
            var name = _console.Ask<string>(
                string.Format(SR.Current.TableEntityNamePrompt, table.Schema, table.TableName)).Trim();

            if (string.IsNullOrWhiteSpace(name) || !char.IsUpper(name[0]) || !name.All(char.IsLetterOrDigit))
            {
                _console.MarkupLine(SR.Current.TableSkipped);
                continue;
            }

            toScaffold.Add((table, name));
        }

        if (toScaffold.Count == 0) return;

        var solutionDir  = Path.GetFullPath(projectName);
        var slnFile      = _fileWriter.FindSolutionFile(solutionDir);
        ScaffoldContext? lastCtx = null;

        foreach (var (table, entityName) in toScaffold)
        {
            IReadOnlyList<EntityProperty> properties;
            try
            {
                properties = _dbSchemaReader.ReadColumns(connectionString, table.Schema, table.TableName, dbFlavor);
            }
            catch (Exception ex)
            {
                _console.MarkupLine(string.Format(SR.Current.ErrorReadingTable, Markup.Escape(ex.Message)));
                continue;
            }

            if (properties.Count == 0)
            {
                _console.MarkupLine(string.Format(SR.Current.NoColumnsFound, table.Schema, table.TableName));
                continue;
            }

            var ctx = new ScaffoldContext(entityName, projectName, solutionDir)
            {
                Properties = properties,
                DbFlavor   = dbFlavor,
                TableName  = table.TableName,
            };

            foreach (var (path, content) in new ScaffoldGenerator(ctx).GetFiles())
            {
                try
                {
                    _fileWriter.EnsureDirectory(Path.GetDirectoryName(path)!);
                    if (!_fileWriter.FileExists(path))
                        _fileWriter.WriteAllText(path, content);
                }
                catch { /* continue */ }
            }

            new DbContextEditor(_fileWriter).InjectDbSet(ctx);

            if (slnFile is not null && _fileWriter.FileExists(ctx.TestsCsprojPath))
                await _dotNetRunner.RunAsync($"sln \"{slnFile}\" add \"{ctx.TestsCsprojPath}\"", cancellationToken);

            lastCtx = ctx;
        }

        if (lastCtx is null) return;

        _console.MarkupLine(string.Format(SR.Current.BulkScaffoldSuccess, toScaffold.Count));

        new EfMigrationRunner(_dotNetRunner, _fileWriter, _console)
            .RunBulkReconciliationMigration(lastCtx, "InitialModel");
    }

    public static void UpdateAppSettings(string projectName, IDbTemplateStrategy strategy, ProjectSetupConfig config, IFileWriter fileWriter)
    {
        var basePath = Path.Combine(projectName, ApiSourceDir, ApiProjectDir);
        var connectionString = strategy.BuildConnectionString(config.DbName, config.DbServer, config.DbUser, config.DbPassword);

        foreach (var fileName in AppSettingsFiles)
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

        if (json[JsonSectionConnectionStrings] is JsonObject connStrings)
            connStrings[connectionKey] = connectionString;
        else
            json[JsonSectionConnectionStrings] = new JsonObject { [connectionKey] = connectionString };

        SetLicenseKey(json, MediatRKeys,    config.MediatrLicense);
        SetLicenseKey(json, AutoMapperKeys, config.AutomapperLicense);

        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static void SetLicenseKey(JsonNode json, string[] keyVariants, string licenseKey)
    {
        foreach (var key in keyVariants)
        {
            if (json[key] is JsonObject node)
            {
                node[JsonKeyLicenseKey] = licenseKey;
                return;
            }
        }

        json[keyVariants[0]] = new JsonObject { [JsonKeyLicenseKey] = licenseKey };
    }
}
