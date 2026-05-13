using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers.Database;

public interface IDbFlavorDetector
{
    DbFlavor Detect(string solutionDir);
}
