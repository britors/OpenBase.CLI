namespace OpenBase.CLI.Commands.Extension;

public record ExtensionContext(
    string CsprojPath,
    string ProjectDir,
    string? Provider,
    IReadOnlyList<string> InstalledPackages);
