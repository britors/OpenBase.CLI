using Microsoft.Extensions.DependencyInjection;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers;
using OpenBase.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddSingleton<IDotNetRunner, DotNetRunner>();
services.AddSingleton<ITemplatePackageRunner, TemplatePackageRunner>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("openbase");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Instala o ecossistema de templates OpenBase.")
        .WithExample("install");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Sincroniza e atualiza todos os templates OpenBase.");

    config.AddCommand<NewCommand>("new")
        .WithDescription("Cria um novo projeto baseado em um template.")
        .WithExample("new", "--type", "api", "--template", "sqlserver", "--name", "MeuProjeto");

    config.AddCommand<HelpCommand>("help")
        .WithDescription("Exibe a ajuda para os comandos do OpenBase");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Exibe as versões da CLI e do template do OpenBase");
});

return await app.RunAsync(args);
