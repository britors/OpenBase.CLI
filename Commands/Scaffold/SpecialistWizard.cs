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

        var sql = console.Ask<string>(SR.Current.SpecialistSqlPrompt);
        var paramNames = SpecialistParam.ExtractNames(sql);

        if (paramNames.Count == 0)
            return new SpecialistDefinition(methodName, type, sql, []);

        console.MarkupLine(string.Format(SR.Current.SpecialistParamsDetected,
            string.Join(", ", paramNames.Select(n => $"[blue]{{{{{n}}}}}[/]"))));

        var parameters = paramNames
            .Select(name => new SpecialistParam(name, AskParamCsType(name)))
            .ToList();

        return new SpecialistDefinition(methodName, type, sql, parameters);
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

    private string AskParamCsType(string paramName) =>
        console.Prompt(
            new SelectionPrompt<string>()
                .Title(string.Format(SR.Current.SpecialistParamTypePrompt, paramName))
                .AddChoices("int", "string", "bool", "decimal", "Guid", "DateTime", "long", "double", "float", "short"));
}
