using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

public interface IEntityPropertyCollector
{
    IReadOnlyList<EntityProperty> Collect(DbFlavor dbFlavor);
}
