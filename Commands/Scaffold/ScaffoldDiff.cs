using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Scaffold;

public sealed record ScaffoldDiff(
    IReadOnlyList<EntityProperty> Added,
    IReadOnlyList<EntityProperty> Removed,
    IReadOnlyList<(EntityProperty Old, EntityProperty New)> Changed)
{
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;

    public static ScaffoldDiff Compute(
        IReadOnlyList<EntityProperty> oldProps,
        IReadOnlyList<EntityProperty> newProps)
    {
        var oldByName = oldProps.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var newByName = newProps.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var added = newProps.Where(p => !oldByName.ContainsKey(p.Name)).ToList();
        var removed = oldProps.Where(p => !newByName.ContainsKey(p.Name)).ToList();
        var changed = newProps
            .Where(p => oldByName.TryGetValue(p.Name, out var old) &&
                        (old.CsType != p.CsType || old.IsRequired != p.IsRequired))
            .Select(p => (oldByName[p.Name], p))
            .ToList();

        return new ScaffoldDiff(added, removed, changed);
    }
}
