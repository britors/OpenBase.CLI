using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Scaffold;

internal sealed class SpecialistWizard(IAnsiConsole console)
{
    private static readonly HashSet<string> Reserved = new(
        ["Create", "Update", "Delete", "FindById", "Get"],
        StringComparer.OrdinalIgnoreCase);

    public SpecialistDefinition? AskDefinition()
    {
        var methodName = AskMethodName();
        if (methodName is null) return null;

        var type = AskType();

        if (type == SpecialistType.HttpCall)
            return new SpecialistDefinition(methodName, type, string.Empty, []);

        var sql        = console.Ask<string>(SR.Current.SpecialistSqlPrompt);
        var paramNames = SpecialistParam.ExtractNames(sql);

        if (paramNames.Count > 0)
            console.MarkupLine(string.Format(SR.Current.SpecialistParamsDetected,
                string.Join(", ", paramNames.Select(n => $"[blue]{{{{{n}}}}}[/]"))));

        var parameters = paramNames
            .Select(name => new SpecialistParam(name, AskCsType(SR.Current.SpecialistParamTypePrompt, name)))
            .ToList();

        if (type == SpecialistType.Command)
            return new SpecialistDefinition(methodName, type, sql, parameters);

        var isPaginated    = console.Confirm(SR.Current.SpecialistPaginatedPrompt, defaultValue: false);
        var resultColumns  = AskResultColumns();

        return new SpecialistDefinition(methodName, type, sql, parameters)
        {
            IsPaginated   = isPaginated,
            ResultColumns = resultColumns,
        };
    }

    private List<SpecialistParam> AskResultColumns()
    {
        var columns = new List<SpecialistParam>();
        console.MarkupLine(SR.Current.SpecialistResultColumnsTitle);

        while (true)
        {
            var prompt = string.Format(SR.Current.SpecialistResultColumnName, columns.Count + 1);
            var name   = console.Prompt(new TextPrompt<string>(prompt).AllowEmpty());

            if (string.IsNullOrWhiteSpace(name)) break;

            if (!char.IsUpper(name[0]))
                name = char.ToUpperInvariant(name[0]) + name[1..];

            var csType = AskCsType(SR.Current.SpecialistResultColumnTypePrompt, name);
            columns.Add(new SpecialistParam(name, csType));
        }

        return columns;
    }

    private string? AskMethodName()
    {
        while (true)
        {
            var name = console.Ask<string>(SR.Current.SpecialistMethodNamePrompt);

            if (string.IsNullOrWhiteSpace(name))
            {
                console.MarkupLine($"[red]{SR.Current.SpecialistMethodRequired}[/]");
                continue;
            }
            if (!char.IsUpper(name[0]))
            {
                console.MarkupLine($"[red]{SR.Current.SpecialistMethodPascalCase}[/]");
                continue;
            }
            if (!name.All(char.IsLetterOrDigit))
            {
                console.MarkupLine($"[red]{SR.Current.SpecialistMethodAlphanumeric}[/]");
                continue;
            }
            if (Reserved.Contains(name))
            {
                console.MarkupLine($"[red]{string.Format(SR.Current.SpecialistMethodReserved, name)}[/]");
                continue;
            }

            return name;
        }
    }

    private SpecialistType AskType()
    {
        var choice = console.Prompt(
            new SelectionPrompt<string>()
                .Title(SR.Current.SpecialistTypePrompt)
                .AddChoices(SR.Current.SpecialistQueryChoice, SR.Current.SpecialistCommandChoice, SR.Current.SpecialistHttpCallChoice));

        if (choice == SR.Current.SpecialistCommandChoice) return SpecialistType.Command;
        if (choice == SR.Current.SpecialistHttpCallChoice) return SpecialistType.HttpCall;
        return SpecialistType.Query;
    }

    private string AskCsType(string promptTemplate, string name) =>
        console.Prompt(
            new SelectionPrompt<string>()
                .Title(string.Format(promptTemplate, name))
                .AddChoices("string", "int", "bool", "decimal", "Guid", "DateTime", "long", "double", "float", "short"));
}
