using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Interactive;

public interface IEntityPropertyCollector
{
    IReadOnlyList<EntityProperty> Collect(DbFlavor dbFlavor);
}
