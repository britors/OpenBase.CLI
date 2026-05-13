using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class ConsoleProjectConfigurator(IAnsiConsole console) : IProjectConfigurator
{
    public ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName)
    {
        console.WriteLine();
        console.MarkupLine(SR.Current.ProjectConfiguration);
        console.WriteLine();

        var mediatrLicense = console.Prompt(
            new TextPrompt<string>(SR.Current.MediatRLicense)
                .AllowEmpty());

        var automapperLicense = console.Prompt(
            new TextPrompt<string>(SR.Current.AutoMapperLicense)
                .AllowEmpty());

        var server = console.Prompt(
            new TextPrompt<string>(SR.Current.DatabaseServer)
                .DefaultValue(strategy.DefaultServer));

        var dbName = console.Prompt(
            new TextPrompt<string>(SR.Current.DatabaseName)
                .DefaultValue(projectName));

        var user = console.Prompt(
            new TextPrompt<string>(SR.Current.DatabaseUser)
                .AllowEmpty());

        var password = console.Prompt(
            new TextPrompt<string>(SR.Current.DatabasePassword)
                .Secret()
                .AllowEmpty());

        return new ProjectSetupConfig(mediatrLicense, automapperLicense, server, user, password, dbName);
    }
}
