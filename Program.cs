using Microsoft.Extensions.DependencyInjection;
using OpenBase.CLI.Helpers.Creation;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Commands.Extension;
using OpenBase.CLI.Commands.Extension.DomainEvents;
using OpenBase.CLI.Commands.Extension.HealthChecks;
using OpenBase.CLI.Commands.Extension.Jwt;
using OpenBase.CLI.Commands.Extension.MongoDB;
using OpenBase.CLI.Commands.Extension.Redis;
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
services.AddSingleton<IPackageCatalog, PackageCatalog>();
services.AddSingleton<PackageOperations>();
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
services.AddSingleton<IExtensionHandler, RedisCacheExtensionHandler>();
services.AddSingleton<IExtensionHandler, MongoDbExtensionHandler>();
services.AddSingleton<IExtensionHandler, DomainEventsExtensionHandler>();
services.AddSingleton<IBrowserLauncher, BrowserLauncher>();

const string TypeOpt = "--type";
const string BuildCmd = "build";
const string RunCmd = "run";
const string VersionCmd = "version";
const string ExtensionCmd = "extension";

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("openbase");
    config.UseStrictParsing();
    config.SetExceptionHandler((error, _) => CommandResult.Write(AnsiConsole.Console,
        args.Contains("--json"), args.FirstOrDefault() ?? "help", error: error switch
        {
            CliException known => known,
            OperationCanceledException => new CliException("CANCELLED", "Operação cancelada.", 130),
            CommandParseException => new CliException("ARGUMENT_INVALID", "Argumentos inválidos. Consulte --help."),
            _ => new CliException("EXECUTION_FAILED", "A operação falhou.", 4)
        }));

    config.AddCommand<BuildCommand>(BuildCmd)
        .WithDescription(SR.Current.CmdBuildDescription)
        .WithExample(BuildCmd)
        .WithExample(BuildCmd, "--configuration", "Release")
        .WithExample(BuildCmd, "--no-restore");

    config.AddCommand<RunCommand>(RunCmd)
        .WithDescription(SR.Current.CmdRunDescription)
        .WithExample(RunCmd)
        .WithExample(RunCmd, "--configuration", "Release")
        .WithExample(RunCmd, "--no-build");

    config.AddCommand<InstallCommand>("install")
        .WithDescription(SR.Current.CmdInstallDescription)
        .WithExample("install");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription(SR.Current.CmdUpdateDescription);

    config.AddCommand<NewCommand>("new")
        .WithDescription(SR.Current.CmdNewDescription)
        .WithExample("new", "--database", "postgres", "--name", "MeuProjeto");

    config.AddCommand<ScaffoldCommand>("scaffold")
        .WithDescription(SR.Current.CmdScaffoldDescription)
        .WithExample("scaffold", "--entity", "Produto");

    config.AddCommand<SpecialistCommand>("specialist")
        .WithDescription(SR.Current.CmdSpecialistDescription)
        .WithExample("specialist", "--entity", "Produto");

    config.AddCommand<ProcedureCommand>("procedure")
        .WithDescription(SR.Current.CmdProcedureDescription)
        .WithExample("procedure", "--name", "GetOrderById")
        .WithExample("procedure", "--name", "GetOrderById", "--schema", "dbo");

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

        extension.AddCommand<ExtensionListCommand>("list")
                 .WithDescription(SR.Current.CmdExtensionListDescription)
                 .WithExample(ExtensionCmd, "list");
    });
});

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
// The legacy generators do not understand the new layout yet (#13).
if (args.FirstOrDefault() is "scaffold" or "specialist" or "procedure" or "extension")
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        var manifest = Path.Combine(dir.FullName, ".openbase.json");
        if (!File.Exists(manifest)) continue;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifest));
            if (document.RootElement.TryGetProperty("schemaVersion", out _))
                return CommandResult.Write(AnsiConsole.Console, args.Contains("--json"), args[0],
                    error: new CliException("CAPABILITY_UNAVAILABLE", "Este gerador ainda suporta somente o layout legado.", 3));
        }
        catch (System.Text.Json.JsonException)
        { return CommandResult.Write(AnsiConsole.Console, args.Contains("--json"), args[0], error: new CliException("MANIFEST_INVALID", "Manifesto inválido.")); }
        break;
    }
}
return await app.RunAsync(args, cancellation.Token);
