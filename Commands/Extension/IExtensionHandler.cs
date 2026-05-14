namespace OpenBase.CLI.Commands.Extension;

public interface IExtensionHandler
{
    string Name { get; }
    IReadOnlyList<string> SupportedProviders { get; }
    ExtensionApplyResult Apply(ExtensionContext context);
}

public record ExtensionApplyResult(bool Success, string? ErrorMessage = null);
