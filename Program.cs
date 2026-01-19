using OpenBaseNetSqlServerCLI.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("openbase");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Instala o ecossistema de templates OpenBaseNET.")
        .WithExample("install");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Sincroniza e atualiza todos os templates OpenBaseNET.");

    config.AddCommand<NewCommand>("new")
            .WithDescription("Cria um novo projeto baseado em um template.")
            .WithExample("new", "--type", "api", "--template", "sqlserver", "--name", "MeuProjeto");

    config.AddCommand<HelpCommand>("help")
        .WithDescription("Exibe a ajuda para os comandos do OpenBaseNET");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Exibe as versões da CLI e do template do OpenBaseNET");

});

return await app.RunAsync(args);