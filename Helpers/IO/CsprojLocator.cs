namespace OpenBase.CLI.Helpers.IO;

public sealed class CsprojLocator : ICsprojLocator
{
    public string? Find(string workingDir)
    {
        var dir = new DirectoryInfo(workingDir);
        for (var depth = 0; depth < 4 && dir is not null; depth++, dir = dir.Parent)
        {
            var files = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 1) return files[0].FullName;
        }
        return null;
    }
}
