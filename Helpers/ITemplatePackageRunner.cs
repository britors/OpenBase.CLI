namespace OpenBase.CLI.Helpers;

public interface ITemplatePackageRunner
{
    Task<IReadOnlyList<(string PackageId, bool Success)>> RunPackagesAsync(string statusVerb, string successLabel, string errorLabel, CancellationToken cancellationToken);
}
