using System.Xml.Linq;

namespace OpenBase.CLI.Helpers.IO;

public sealed class CsprojPackageReader : ICsprojPackageReader
{
    public IReadOnlyList<string> ReadPackages(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();
    }
}
