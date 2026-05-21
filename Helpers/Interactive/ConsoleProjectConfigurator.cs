using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

public sealed class ConsoleProjectConfigurator(IAnsiConsole console) : IProjectConfigurator
{
    public ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName, ProjectSetupOverrides? overrides = null)
    {
        var hasAnyOverride = overrides is not null && (
            !string.IsNullOrWhiteSpace(overrides.MediatrLicense) ||
            !string.IsNullOrWhiteSpace(overrides.AutomapperLicense) ||
            !string.IsNullOrWhiteSpace(overrides.DbServer) ||
            !string.IsNullOrWhiteSpace(overrides.DbName) ||
            !string.IsNullOrWhiteSpace(overrides.DbUser) ||
            !string.IsNullOrWhiteSpace(overrides.DbPassword));

        if (!hasAnyOverride)
        {
            console.WriteLine();
            console.MarkupLine(SR.Current.ProjectConfiguration);
            console.WriteLine();
        }

        var mediatrLicense = !string.IsNullOrWhiteSpace(overrides?.MediatrLicense)
            ? overrides.MediatrLicense
            : console.Prompt(new TextPrompt<string>(SR.Current.MediatRLicense).AllowEmpty());

        var automapperLicense = !string.IsNullOrWhiteSpace(overrides?.AutomapperLicense)
            ? overrides.AutomapperLicense
            : console.Prompt(new TextPrompt<string>(SR.Current.AutoMapperLicense).AllowEmpty());

        var server = !string.IsNullOrWhiteSpace(overrides?.DbServer)
            ? overrides.DbServer
            : console.Prompt(new TextPrompt<string>(SR.Current.DatabaseServer).DefaultValue(strategy.DefaultServer));

        var dbName = !string.IsNullOrWhiteSpace(overrides?.DbName)
            ? overrides.DbName
            : console.Prompt(new TextPrompt<string>(SR.Current.DatabaseName).DefaultValue(projectName));

        var user = !string.IsNullOrWhiteSpace(overrides?.DbUser)
            ? overrides.DbUser
            : console.Prompt(new TextPrompt<string>(SR.Current.DatabaseUser).AllowEmpty());

        var password = overrides?.DbPassword is not null
            ? overrides.DbPassword
            : console.Prompt(new TextPrompt<string>(SR.Current.DatabasePassword).Secret().AllowEmpty());

        return new ProjectSetupConfig(mediatrLicense, automapperLicense, server, user, password, dbName);
    }
}
