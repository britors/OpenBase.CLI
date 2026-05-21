using OpenBase.CLI.Helpers.Database;

namespace OpenBase.CLI.Helpers.Interactive;

public record ProjectSetupConfig(
    string MediatrLicense,
    string AutomapperLicense,
    string DbServer,
    string DbUser,
    string DbPassword,
    string DbName
);

public record ProjectSetupOverrides(
    string? MediatrLicense = null,
    string? AutomapperLicense = null,
    string? DbServer = null,
    string? DbName = null,
    string? DbUser = null,
    string? DbPassword = null);

public interface IProjectConfigurator
{
    ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName, ProjectSetupOverrides? overrides = null);
}
