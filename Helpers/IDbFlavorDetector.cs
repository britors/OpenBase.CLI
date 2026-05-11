using OpenBase.CLI.Models;

namespace OpenBase.CLI.Helpers;

public interface IDbFlavorDetector
{
    DbFlavor Detect(string solutionDir);
}
