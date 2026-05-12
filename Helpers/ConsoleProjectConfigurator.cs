using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class ConsoleProjectConfigurator(IAnsiConsole console) : IProjectConfigurator
{
    public ProjectSetupConfig Collect(IDbTemplateStrategy strategy, string projectName)
    {
        console.WriteLine();
        console.MarkupLine("[bold]Configuração do projeto[/]");
        console.WriteLine();

        var mediatrLicense = console.Prompt(
            new TextPrompt<string>("Licença do [blue]MediatR[/] [grey](deixe em branco se não tiver)[/]:")
                .AllowEmpty());

        var automapperLicense = console.Prompt(
            new TextPrompt<string>("Licença do [blue]AutoMapper[/] [grey](deixe em branco se não tiver)[/]:")
                .AllowEmpty());

        var server = console.Prompt(
            new TextPrompt<string>("Servidor do banco de dados:")
                .DefaultValue(strategy.DefaultServer));

        var dbName = console.Prompt(
            new TextPrompt<string>("Nome do banco de dados:")
                .DefaultValue(projectName));

        var user = console.Prompt(
            new TextPrompt<string>("Usuário do banco de dados:")
                .AllowEmpty());

        var password = console.Prompt(
            new TextPrompt<string>("Senha do banco de dados:")
                .Secret()
                .AllowEmpty());

        return new ProjectSetupConfig(mediatrLicense, automapperLicense, server, user, password, dbName);
    }
}
