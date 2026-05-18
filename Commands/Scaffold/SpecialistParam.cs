using System.Text.RegularExpressions;

namespace OpenBase.CLI.Commands.Scaffold;

public sealed record SpecialistParam(string Name, string CsType)
{
    private static readonly Regex TemplateRegex = new(
        @"\{\{\s*(?<name>\w+)\s*\}\}",
        RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromSeconds(5));

    public string PascalName => char.ToUpperInvariant(Name[0]) + Name[1..];
    public string CamelName  => char.ToLowerInvariant(Name[0]) + Name[1..];

    public static IReadOnlyList<string> ExtractNames(string template) =>
        TemplateRegex.Matches(template)
            .Select(m => m.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string ToParameterizedSql(string template, IReadOnlyList<SpecialistParam> parameters) =>
        TemplateRegex.Replace(template, m =>
        {
            var paramName = m.Groups["name"].Value;
            var param = parameters.FirstOrDefault(p =>
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            return param is not null ? $"@{param.PascalName}" : m.Value;
        });
}

public sealed record SpecialistDefinition(
    string MethodName,
    SpecialistType Type,
    string Sql,
    IReadOnlyList<SpecialistParam> Parameters);
