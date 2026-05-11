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
services.AddSingleton<IUpdateHistoryService, UpdateHistoryService>();
services.AddSingleton<IProjectLocator, ProjectLocator>();
services.AddSingleton<IFileWriter, FileWriter>();
services.AddSingleton<IProjectConfigurator, ConsoleProjectConfigurator>();
services.AddSingleton<IEntityPropertyCollector, ConsoleEntityPropertyCollector>();
services.AddSingleton<IDbFlavorDetector, DbFlavorDetector>();

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

    config.AddCommand<ScaffoldCommand>("scaffold")
        .WithDescription("Gera todas as camadas da arquitetura para uma entidade.")
        .WithExample("scaffold", "--entity", "Produto");

    config.AddCommand<HistoryCommand>("history")
        .WithDescription("Exibe o histórico de atualizações dos componentes OpenBase.")
        .WithExample("history")
        .WithExample("history", "--type", "cli");

    config.AddCommand<HelpCommand>("help")
        .WithDescription("Exibe a ajuda para os comandos do OpenBase");

    config.AddBranch<CommandSettings>("version", version =>
    {
        version.SetDescription("Exibe e gerencia versões dos componentes OpenBase.");

        version.AddCommand<VersionCommand>("show")
               .WithDescription("Exibe as versões da CLI e do template do OpenBase")
               .WithExample("version", "show");

        version.AddCommand<VersionRestoreCommand>("restore")
               .WithDescription("Restaura um componente para uma versão específica.")
               .WithExample("version", "restore", "10.5.9", "--type", "cli")
               .WithExample("version", "restore", "2.0.0", "--type", "sqlserver");
    });
});

return await app.RunAsync(args);
