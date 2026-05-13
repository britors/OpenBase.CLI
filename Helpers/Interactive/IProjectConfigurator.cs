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

public interface IProjectConfigurator
{
    ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName);
}
