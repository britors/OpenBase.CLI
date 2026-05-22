using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

public sealed class ConsoleProjectConfigurator(IAnsiConsole console) : IProjectConfigurator
{
    public ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName, ProjectSetupOverrides? overrides = null)
    {
        var interactive = console.Profile.Capabilities.Interactive;

        if (interactive)
        {
            console.WriteLine();
            console.MarkupLine(SR.Current.ProjectConfiguration);
            console.WriteLine();
        }

        var mediatrLicense = !string.IsNullOrWhiteSpace(overrides?.MediatrLicense)
            ? overrides.MediatrLicense
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.MediatRLicense).AllowEmpty()) : string.Empty;

        var automapperLicense = !string.IsNullOrWhiteSpace(overrides?.AutomapperLicense)
            ? overrides.AutomapperLicense
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.AutoMapperLicense).AllowEmpty()) : string.Empty;

        var server = !string.IsNullOrWhiteSpace(overrides?.DbServer)
            ? overrides.DbServer
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.DatabaseServer).DefaultValue(strategy.DefaultServer)) : strategy.DefaultServer;

        var dbName = !string.IsNullOrWhiteSpace(overrides?.DbName)
            ? overrides.DbName
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.DatabaseName).DefaultValue(projectName)) : projectName;

        var user = !string.IsNullOrWhiteSpace(overrides?.DbUser)
            ? overrides.DbUser
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.DatabaseUser).AllowEmpty()) : string.Empty;

        var password = overrides?.DbPassword is not null
            ? overrides.DbPassword
            : interactive ? console.Prompt(new TextPrompt<string>(SR.Current.DatabasePassword).Secret().AllowEmpty()) : string.Empty;

        return new ProjectSetupConfig(mediatrLicense, automapperLicense, server, user, password, dbName);
    }
}
