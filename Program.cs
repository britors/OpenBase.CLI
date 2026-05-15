using Microsoft.Extensions.DependencyInjection;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.HealthChecks;
using OpenBase.CLI.Commands.Extension.Jwt;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Helpers.Interactive;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Infrastructure;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

SR.Configure();

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
services.AddSingleton<IDbSchemaReader, DbSchemaReader>();
services.AddSingleton<IConnectionStringReader, AppSettingsConnectionStringReader>();
services.AddSingleton<IModelFirstPropertyCollector, ConsoleModelFirstPropertyCollector>();
services.AddSingleton<ICsprojLocator, CsprojLocator>();
services.AddSingleton<ICsprojPackageReader, CsprojPackageReader>();
services.AddSingleton<IExtensionRegistry, ExtensionRegistry>();
services.AddSingleton<IExtensionHandler, JwtExtensionHandler>();
services.AddSingleton<IExtensionHandler, HealthChecksExtensionHandler>();

const string TypeOpt = "--type";
const string VersionCmd = "version";
const string ExtensionCmd = "extension";

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("openbase");

    config.AddCommand<InstallCommand>("install")
        .WithDescription(SR.Current.CmdInstallDescription)
        .WithExample("install");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription(SR.Current.CmdUpdateDescription);

    config.AddCommand<NewCommand>("new")
        .WithDescription(SR.Current.CmdNewDescription)
        .WithExample("new", TypeOpt, "api", "--template", "sqlserver", "--name", "MeuProjeto");

    config.AddCommand<ScaffoldCommand>("scaffold")
        .WithDescription(SR.Current.CmdScaffoldDescription)
        .WithExample("scaffold", "--entity", "Produto");

    config.AddCommand<HistoryCommand>("history")
        .WithDescription(SR.Current.CmdHistoryDescription)
        .WithExample("history")
        .WithExample("history", TypeOpt, "cli");

    config.AddCommand<HelpCommand>("help")
        .WithDescription(SR.Current.CmdHelpDescription);

    config.AddBranch<CommandSettings>(VersionCmd, version =>
    {
        version.SetDescription(SR.Current.CmdVersionDescription);

        version.AddCommand<VersionCommand>("show")
               .WithDescription(SR.Current.CmdVersionShowDescription)
               .WithExample(VersionCmd, "show");

        version.AddCommand<VersionRestoreCommand>("restore")
               .WithDescription(SR.Current.CmdVersionRestoreDescription)
               .WithExample(VersionCmd, "restore", "10.5.9", TypeOpt, "cli")
               .WithExample(VersionCmd, "restore", "2.0.0", TypeOpt, "sqlserver");
    });

    config.AddBranch<CommandSettings>(ExtensionCmd, extension =>
    {
        extension.SetDescription(SR.Current.CmdExtensionDescription);

        extension.AddCommand<ExtensionAddCommand>("add")
                 .WithDescription(SR.Current.CmdExtensionAddDescription)
                 .WithExample(ExtensionCmd, "add", "jwt")
                 .WithExample(ExtensionCmd, "add", "cache", "--provider", "redis");
    });
});

return await app.RunAsync(args);
