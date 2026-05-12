namespace OpenBase.CLI.Helpers;

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
