using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Interactive;

public interface IModelFirstPropertyCollector
{
    (IReadOnlyList<EntityProperty> Properties, string TableName)? Collect(
        string solutionDir, string rootNamespace, DbFlavor dbFlavor,
        string? schemaOverride = null, string? tableOverride = null);
}
