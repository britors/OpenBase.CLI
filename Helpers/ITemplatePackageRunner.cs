namespace OpenBase.CLI.Helpers;

public interface ITemplatePackageRunner
{
    Task<bool> RunPackagesAsync(string statusVerb, string successLabel, string errorLabel, CancellationToken cancellationToken);
}
