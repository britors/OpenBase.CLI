using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

public interface IModelFirstPropertyCollector
{
    IReadOnlyList<EntityProperty>? Collect(string solutionDir, string rootNamespace, DbFlavor dbFlavor);
}
