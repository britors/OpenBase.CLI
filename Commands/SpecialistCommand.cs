using System.ComponentModel;
using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class SpecialistSettings : EntityCommandSettings
{
    [CommandOption("--method <NOME>")]
    [Description("Nome do método especialista (PascalCase, ex: GetByCategoria)")]
    public string? Method { get; set; }

    [CommandOption("--type <TIPO>")]
    [Description("Tipo do especialista: query (padrão), command ou httpcall")]
    public string? Type { get; set; }

    [CommandOption("--sql <SQL>")]
    [Description("Template SQL com parâmetros no formato {{paramNome}}")]
    public string? Sql { get; set; }

    [CommandOption("--paginated")]
    [Description("Gera query paginada (somente para tipo query)")]
    public bool Paginated { get; set; }

    [CommandOption("--param <NOME:TIPO>")]
    [Description("Parâmetro SQL no formato nome:Tipo (ex: categoriaId:Guid). Repetível.")]
    public string[]? Params { get; set; }

    [CommandOption("--column <NOME:TIPO>")]
    [Description("Coluna de resultado no formato Nome:Tipo (ex: Nome:string). Repetível.")]
    public string[]? Columns { get; set; }
}

public class SpecialistCommand(
    IAnsiConsole console,
    IProjectLocator projectLocator,
    IFileWriter fileWriter)
    : Command<SpecialistSettings>
{
    private static readonly HashSet<string> ValidTypes =
        new(["query", "command", "httpcall"], StringComparer.OrdinalIgnoreCase);

    protected override int Execute(CommandContext context, SpecialistSettings settings, CancellationToken cancellationToken)
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

        var ctx       = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir);
        var generator = new ScaffoldGenerator(ctx);
        var helper    = new ScaffoldFileHelper(console, fileWriter);

        if (settings.Method is not null)
            return ExecuteNonInteractive(settings, generator, helper, solutionDir);

        if (!console.Profile.Capabilities.Interactive)
        {
            console.MarkupLine($"[red]{SR.Current.SpecialistMethodRequired}[/]");
            return 1;
        }

        var wizard = new SpecialistWizard(console);

        do
        {
            var definition = wizard.AskDefinition();
            if (definition is null) return 0;

            WriteAndPrint(generator, helper, definition, solutionDir, settings.Entity);

        } while (console.Confirm(SR.Current.SpecialistAddAnother, defaultValue: false));

        return 0;
    }

    private int ExecuteNonInteractive(SpecialistSettings settings, ScaffoldGenerator generator, ScaffoldFileHelper helper, string solutionDir)
    {
        var type = ResolveType(settings.Type);
        if (type is null) return 1;

        var definition = BuildDefinition(settings, type.Value);
        if (definition is null) return 1;

        return WriteAndPrint(generator, helper, definition, solutionDir, settings.Entity);
    }

    private SpecialistType? ResolveType(string? raw)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? "query" : raw;

        if (!ValidTypes.Contains(value))
        {
            console.MarkupLine($"[red]{string.Format(SR.Current.SpecialistTypeInvalid, value)}[/]");
            return null;
        }

        if (value.Equals("command",  StringComparison.OrdinalIgnoreCase)) return SpecialistType.Command;
        if (value.Equals("httpcall", StringComparison.OrdinalIgnoreCase)) return SpecialistType.HttpCall;
        return SpecialistType.Query;
    }

    private SpecialistDefinition? BuildDefinition(SpecialistSettings settings, SpecialistType type)
    {
        if (type == SpecialistType.HttpCall)
            return new SpecialistDefinition(settings.Method!, type, string.Empty, []);

        if (string.IsNullOrWhiteSpace(settings.Sql))
        {
            console.MarkupLine($"[red]{SR.Current.SpecialistSqlRequired}[/]");
            return null;
        }

        var paramMap = ParsePairs(settings.Params, SR.Current.SpecialistParamFormatInvalid);
        if (paramMap is null) return null;

        var paramNames = SpecialistParam.ExtractNames(settings.Sql);
        var parameters = paramNames
            .Select(name => new SpecialistParam(name, paramMap.GetValueOrDefault(name, "string")))
            .ToList();

        if (type == SpecialistType.Command)
            return new SpecialistDefinition(settings.Method!, type, settings.Sql, parameters);

        var columnMap = ParsePairs(settings.Columns, SR.Current.SpecialistColumnFormatInvalid);
        if (columnMap is null) return null;

        var columns = columnMap
            .Select(kv => new SpecialistParam(kv.Key, kv.Value))
            .ToList();

        return new SpecialistDefinition(settings.Method!, type, settings.Sql, parameters)
        {
            IsPaginated   = settings.Paginated,
            ResultColumns = columns,
        };
    }

    private Dictionary<string, string>? ParsePairs(string[]? flags, string errorTemplate)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var flag in flags ?? [])
        {
            var idx = flag.IndexOf(':');
            if (idx <= 0 || idx == flag.Length - 1)
            {
                console.MarkupLine($"[red]{string.Format(errorTemplate, Markup.Escape(flag))}[/]");
                return null;
            }

            result[flag[..idx].Trim()] = flag[(idx + 1)..].Trim();
        }

        return result;
    }

    private int WriteAndPrint(ScaffoldGenerator generator, ScaffoldFileHelper helper, SpecialistDefinition definition, string solutionDir, string entityName)
    {
        var files = generator.GetSpecialistFiles(definition).ToList();
        var (created, skipped, failed) = helper.WriteFiles(files, solutionDir, entityName);

        helper.PrintFileList(string.Format(SR.Current.SpecialistFilesCreated, created.Count), created, "green");
        helper.PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");
        if (failed.Count > 0)
            helper.PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");

        return failed.Count > 0 ? 1 : 0;
    }
}
