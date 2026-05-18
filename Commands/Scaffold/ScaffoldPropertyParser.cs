using System.Text.RegularExpressions;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Scaffold;

public static class ScaffoldPropertyParser
{
    private static readonly Regex PropertyRegex = new(
        @"public\s+(?<type>[^\s{]+)\s+(?<name>[A-Z]\w*)\s*\{\s*get;\s*init;\s*\}",
        RegexOptions.Compiled | RegexOptions.Multiline,
        matchTimeout: TimeSpan.FromSeconds(5));

    public static IReadOnlyList<EntityProperty> Parse(string entityFileContent)
    {
        var props = new List<EntityProperty>();

        foreach (Match match in PropertyRegex.Matches(entityFileContent))
        {
            var csType = match.Groups["type"].Value;
            var name   = match.Groups["name"].Value;

            if (name == "Id") continue;

            var isNullable = csType.EndsWith('?');
            var baseType   = isNullable ? csType[..^1] : csType;

            // string type without '?' is required only when followed by = string.Empty
            var isRequired = !isNullable && (
                baseType != "string" ||
                entityFileContent[(match.Index + match.Length)..].TrimStart().StartsWith("= string.Empty"));

            props.Add(new EntityProperty(name, baseType, isRequired));
        }

        return props;
    }
}
