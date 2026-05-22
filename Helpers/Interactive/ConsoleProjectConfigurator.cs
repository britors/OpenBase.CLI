using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

public sealed class ConsoleProjectConfigurator(IAnsiConsole console) : IProjectConfigurator
{
    private bool Interactive => console.Profile.Capabilities.Interactive;

    public ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName, ProjectSetupOverrides? overrides = null)
    {
        if (Interactive)
        {
            console.WriteLine();
            console.MarkupLine(SR.Current.ProjectConfiguration);
            console.WriteLine();
        }

        return new ProjectSetupConfig(
            Resolve(overrides?.MediatrLicense,    string.Empty,          () => console.Prompt(new TextPrompt<string>(SR.Current.MediatRLicense).AllowEmpty())),
            Resolve(overrides?.AutomapperLicense, string.Empty,          () => console.Prompt(new TextPrompt<string>(SR.Current.AutoMapperLicense).AllowEmpty())),
            Resolve(overrides?.DbServer,          strategy.DefaultServer, () => console.Prompt(new TextPrompt<string>(SR.Current.DatabaseServer).DefaultValue(strategy.DefaultServer))),
            Resolve(overrides?.DbUser,            string.Empty,          () => console.Prompt(new TextPrompt<string>(SR.Current.DatabaseUser).AllowEmpty())),
            Resolve(overrides?.DbPassword,        string.Empty,          () => console.Prompt(new TextPrompt<string>(SR.Current.DatabasePassword).Secret().AllowEmpty())),
            Resolve(overrides?.DbName,            projectName,           () => console.Prompt(new TextPrompt<string>(SR.Current.DatabaseName).DefaultValue(projectName)))
        );
    }

    private string Resolve(string? overrideValue, string fallback, Func<string> prompt)
    {
        if (!string.IsNullOrWhiteSpace(overrideValue)) return overrideValue;
        return Interactive ? prompt() : fallback;
    }
}
