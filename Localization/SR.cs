using System.Globalization;
using System.Runtime.CompilerServices;

namespace OpenBase.CLI.Localization;

public interface IStrings
{
    string EntityParamRequired { get; }
    string EntityMustBePascalCase { get; }
    string EntityMustBeAlphanumeric { get; }
    string ProjectStructureNotFound { get; }
    string RunInProjectRoot { get; }
    string OrProvideNamespace { get; }
    string ProceedWithScaffold { get; }
    string GeneratingScaffold { get; }        // {0}=entity
    string FilesCreated { get; }              // {0}=count
    string FilesSkipped { get; }              // {0}=count
    string FilesErrors { get; }               // {0}=count
    string ScaffoldSuccess { get; }           // {0}=entity
    string NextSteps { get; }
    string AddDbSet { get; }                  // {0}=entity {1}=plural
    string RunMigrationsAdd { get; }          // {0}=entity
    string RunDatabaseUpdate { get; }
    string RestoringNuGetPackages { get; }
    string RestorePackagesWarning { get; }
    string GeneratingMigration { get; }       // {0}=entity
    string MigrationFailed { get; }
    string RunMigrationManually { get; }      // {0}=entity
    string MigrationGenerated { get; }        // {0}=entity
    string RunDatabaseUpdateNow { get; }
    string ExecutingDatabaseUpdate { get; }
    string DatabaseUpdateFailed { get; }
    string DotnetEfDatabaseUpdate { get; }
    string DatabaseUpdatedSuccess { get; }
    string GeneratingUpdateMigration { get; }       // {0}=entity
    string UpdateMigrationGenerated { get; }        // {0}=entity
    string RunUpdateMigrationManually { get; }      // {0}=entity

    string HowToGenerateScaffold { get; }
    string CodeFirstChoice { get; }
    string ModelFirstChoice { get; }
    string ModelFirstReconciliationInfo { get; }   // {0}=entity
    string ModelFirstReconciliationSuccess { get; }
    string ModelFirstReconciliationWarn { get; }   // {0}=entity

    string NameParamRequired { get; }
    string ProjectNameInvalid { get; }
    string SdkIncompatible { get; }
    string SdkUpdateRequired { get; }         // {0}=version
    string ApiDatabasePrompt { get; }
    string InvalidTypeCombination { get; }    // {0}=type {1}=template
    string AvailableCombinations { get; }
    string CreatingProject { get; }           // {0}=name
    string CreateProjectFailed { get; }

    string ProjectConfiguration { get; }
    string MediatRLicense { get; }
    string AutoMapperLicense { get; }
    string DatabaseServer { get; }
    string DatabaseName { get; }
    string DatabaseUser { get; }
    string DatabasePassword { get; }

    string EntityProperties { get; }
    string DatabaseAndTypes { get; }          // {0}=dbFlavor {1}=types
    string PropertyNamePrompt { get; }        // {0}=count
    string PropertyTypePrompt { get; }
    string PropertyNotNull { get; }
    string PropertyAdded { get; }             // {0}=name {1}=type
    string AddAnotherProperty { get; }
    string ColProperty { get; }
    string ColType { get; }
    string ColPK { get; }
    string ColNotNull { get; }
    string PropNameRequired { get; }
    string PropNameMustStartUpper { get; }
    string PropNameAlphanumericOnly { get; }
    string PropNameIdReserved { get; }
    string PropNameAlreadyAdded { get; }      // {0}=name

    string SchemaOwnerPrompt { get; }         // {0}=defaultSchema
    string TableNamePrompt { get; }
    string TableNameRequired { get; }
    string ConnectionStringNotFound { get; }
    string ConnectionStringRequired { get; }
    string ReadingTableStructure { get; }     // {0}=schema {1}=table
    string ErrorReadingTable { get; }         // {0}=message
    string NoColumnsFound { get; }            // {0}=schema {1}=table
    string CheckSchemaAndTableName { get; }
    string ColCsType { get; }

    string SyncingTemplates { get; }
    string PackageStatusVerb { get; }
    string PackageSuccessLabel { get; }
    string PackageErrorLabel { get; }
    string PackageOperationFailed { get; }    // {0}=errorLabel {1}=packageId
    string PackageOperationSuccess { get; }   // {0}=packageId  {1}=successLabel
    string InstallStarting { get; }
    string InstallStatusVerb { get; }
    string InstallSuccessLabel { get; }
    string InstallErrorLabel { get; }
    string UpdatingCli { get; }
    string UpdateCliFailed { get; }
    string SomeComponentsUpdateFailed { get; }
    string CliUpdated { get; }

    string UseTypeToSpecify { get; }
    string InvalidType { get; }               // {0}=type
    string RestoringToVersion { get; }        // {0}=displayName {1}=version
    string ApplyingVersion { get; }           // {0}=version
    string RestoreFailed { get; }             // {0}=displayName {1}=version
    string RestoreSuccess { get; }            // {0}=displayName {1}=version

    string InvalidTypeHistory { get; }        // {0}=type
    string NoHistoryFound { get; }
    string ColDate { get; }
    string ColComponent { get; }
    string ColPreviousVersion { get; }
    string ColNewVersion { get; }
    string ColStatus { get; }

    string CmdRunDescription { get; }
    string RunNoProjectFound { get; }
    string RunStarting { get; }          // {0}=project name
    string RunSwaggerUrl { get; }        // {0}=url
    string RunOpeningBrowser { get; }
    string RunStopped { get; }
    string HelpRunDesc { get; }

    string CmdBuildDescription { get; }
    string BuildNoProjectFound { get; }
    string BuildRestoring { get; }
    string BuildBuilding { get; }
    string BuildTesting { get; }
    string BuildStepSuccess { get; }
    string BuildStepFailed { get; }
    string BuildSuccess { get; }
    string BuildFailed { get; }
    string HelpBuildDesc { get; }

    string HelpSubtitle { get; }
    string HelpColCommand { get; }
    string HelpColDescription { get; }
    string HelpColExample { get; }
    string HelpInstallDesc { get; }
    string HelpNewDesc { get; }
    string HelpScaffoldDesc { get; }
    string HelpHistoryDesc { get; }
    string HelpUpdateDesc { get; }
    string HelpVersionShowDesc { get; }
    string HelpVersionRestoreDesc { get; }
    string HelpExtensionAddDesc { get; }
    string HelpExtensionListDesc { get; }
    string HelpTip { get; }
    string HelpSupport { get; }

    string ColVersionComponent { get; }
    string ColVersion { get; }

    string CmdInstallDescription { get; }
    string CmdUpdateDescription { get; }
    string CmdNewDescription { get; }
    string CmdScaffoldDescription { get; }
    string CmdHistoryDescription { get; }
    string CmdHelpDescription { get; }
    string CmdVersionDescription { get; }
    string CmdVersionShowDescription { get; }
    string CmdVersionRestoreDescription { get; }

    string ExtensionNoCsprojFound { get; }
    string ExtensionAlreadyInstalled { get; }        // {0}=name
    string ExtensionNotFound { get; }                // {0}=name
    string ExtensionInvalidProvider { get; }         // {0}=provider {1}=name {2}=available
    string ExtensionApplyFailed { get; }             // {0}=name {1}=error
    string ExtensionAddSuccess { get; }              // {0}=name
    string ExtensionRequiresOpenBaseProject { get; }
    string ExtensionAddingPackage { get; }           // {0}=package {1}=csproj
    string ExtensionPackageAddWarning { get; }       // {0}=package {1}=error
    string ExtensionAddingReference { get; }         // {0}=reference {1}=csproj
    string ExtensionReferenceAddWarning { get; }     // {0}=reference {1}=error
    string ExtensionFileSkipped { get; }             // {0}=filename
    string ExtensionFileCreated { get; }             // {0}=relative path
    string CmdSpecialistDescription { get; }
    string HelpSpecialistDesc { get; }

    string CmdExtensionDescription { get; }
    string CmdExtensionAddDescription { get; }
    string CmdExtensionListDescription { get; }

    string ExtensionListColName { get; }
    string ExtensionListColCommand { get; }
    string ExtensionListColStatus { get; }
    string ExtensionListStatusInstalled { get; }
    string ExtensionListStatusAvailable { get; }

    string JwtProgramCsInjected { get; }
    string JwtProgramCsAlreadyConfigured { get; }
    string JwtProgramCsNotFound { get; }
    string JwtProgramCsWarning { get; }              // {0}=error
    string JwtAppSettingsInjected { get; }
    string JwtAppSettingsWarning { get; }            // {0}=error
    string JwtControllerProtected { get; }           // {0}=relative path
    string JwtSwaggerInjected { get; }
    string JwtSwaggerWarning { get; }                // {0}=error

    string HealthChecksProgramCsInjected { get; }
    string HealthChecksProgramCsAlreadyConfigured { get; }
    string HealthChecksProgramCsNotFound { get; }
    string HealthChecksProgramCsWarning { get; }     // {0}=error

    string RedisProgramCsInjected { get; }
    string RedisProgramCsAlreadyConfigured { get; }
    string RedisProgramCsNotFound { get; }
    string RedisProgramCsWarning { get; }            // {0}=error
    string RedisAppSettingsInjected { get; }
    string RedisAppSettingsWarning { get; }          // {0}=error

    string TestingDbConnection { get; }
    string DbConnectionSuccess { get; }
    string DbConnectionFailed { get; }
    string ImportFullModelPrompt { get; }
    string ListingTables { get; }
    string NoTablesFound { get; }
    string TablesFound { get; }                  // {0}=count
    string TableEntityNamePrompt { get; }        // {0}=schema {1}=table
    string TableSkipped { get; }
    string BulkScaffoldSuccess { get; }          // {0}=count

    string SpecialistAddPrompt { get; }
    string SpecialistMethodNamePrompt { get; }
    string SpecialistTypePrompt { get; }
    string SpecialistQueryChoice { get; }
    string SpecialistCommandChoice { get; }
    string SpecialistHttpCallChoice { get; }
    string SpecialistAddAnother { get; }
    string SpecialistMethodRequired { get; }
    string SpecialistMethodPascalCase { get; }
    string SpecialistMethodAlphanumeric { get; }
    string SpecialistMethodReserved { get; }          // {0}=name
    string SpecialistFilesCreated { get; }            // {0}=count
    string SpecialistSqlPrompt { get; }
    string SpecialistParamsDetected { get; }          // {0}=param list
    string SpecialistParamTypePrompt { get; }         // {0}=param name
    string SpecialistPaginatedPrompt { get; }
    string SpecialistResultColumnsTitle { get; }
    string SpecialistResultColumnName { get; }        // {0}=count
    string SpecialistResultColumnTypePrompt { get; }  // {0}=name

    string ScaffoldUpdateEntityNotFound { get; }     // {0}=entity
    string ScaffoldUpdateNoChanges { get; }
    string ScaffoldUpdateDiffTitle { get; }
    string ScaffoldUpdateUncommittedWarn { get; }
    string ScaffoldUpdateUncommittedHint { get; }
    string ScaffoldUpdateApplyChanges { get; }
    string ScaffoldUpdateDestructiveWarn { get; }    // {0}=count
    string ScaffoldUpdateConfirmRemoval { get; }
    string FilesUpdated { get; }                     // {0}=count
    string ScaffoldUpdateSuccess { get; }            // {0}=entity
    string ScaffoldUpdateMigrationHint { get; }      // {0}=entity
}


public abstract class BaseStrings(IReadOnlyDictionary<string, string> overrides) : IStrings
{
    protected string T(string @default, [CallerMemberName] string key = "") =>
        overrides.TryGetValue(key, out var v) ? v : @default;

    public string EntityParamRequired         => T("The --entity <ENTITY> parameter is required.");
    public string EntityMustBePascalCase      => T("The entity name must start with an uppercase letter (PascalCase).");
    public string EntityMustBeAlphanumeric    => T("The entity name must contain only letters and numbers.");
    public string ProjectStructureNotFound    => T("[red]Error:[/] OpenBase project structure not found.");
    public string RunInProjectRoot            => T("Run this command at the root of a project created with [blue]openbase new[/].");
    public string OrProvideNamespace          => T("Or provide the namespace with [blue]--namespace <NAMESPACE>[/].");
    public string ProceedWithScaffold         => T("Proceed with scaffold?");
    public string GeneratingScaffold          => T("Generating scaffold for [blue]{0}[/]...");
    public string FilesCreated                => T("{0} file(s) created:");
    public string FilesSkipped                => T("{0} file(s) already exist, skipped:");
    public string FilesErrors                 => T("{0} error(s):");
    public string ScaffoldSuccess             => T("\n[green]Scaffold for entity [bold]{0}[/] generated successfully![/]");
    public string NextSteps                   => T("Next steps:");
    public string AddDbSet                    => T("  1. Add [blue]DbSet<{0}> {1} {{ get; set; }}[/] to DbContext");
    public string RunMigrationsAdd            => T("  2. Run [blue]dotnet ef migrations add Add{0}[/]");
    public string RunDatabaseUpdate           => T("  3. Run [blue]dotnet ef database update[/]");
    public string RestoringNuGetPackages      => T("Restoring NuGet packages...");
    public string RestorePackagesWarning      => T("[yellow]Warning:[/] Failed to restore packages. Trying to generate the migration anyway...");
    public string GeneratingMigration         => T("Generating migration [blue]Add{0}[/]...");
    public string MigrationFailed             => T("[red]Error:[/] Failed to generate the migration.");
    public string RunMigrationManually        => T("Run manually: [blue]dotnet ef migrations add Add{0}[/]");
    public string MigrationGenerated          => T("[green]Migration Add{0} generated.[/]");
    public string RunDatabaseUpdateNow        => T("Run [blue]database update[/] now?");
    public string ExecutingDatabaseUpdate     => T("Running [blue]database update[/]...");
    public string DatabaseUpdateFailed        => T("[red]Error:[/] Failed to run database update.");
    public string DotnetEfDatabaseUpdate      => "[blue]dotnet ef database update[/]";
    public string DatabaseUpdatedSuccess      => T("[green]Database updated successfully.[/]");
    public string GeneratingUpdateMigration   => T("Generating migration [blue]Update{0}[/]...");
    public string UpdateMigrationGenerated    => T("[green]Migration Update{0} generated.[/]");
    public string RunUpdateMigrationManually  => T("Run manually: [blue]dotnet ef migrations add Update{0}[/]");

    public string HowToGenerateScaffold       => T("\nHow would you like to generate the scaffold?");
    public string CodeFirstChoice             => T("Code First (define properties manually)");
    public string ModelFirstChoice            => T("Model First (read structure from an existing table)");
    public string ModelFirstReconciliationInfo    => T("Registering [blue]Add{0}[/] in EF migration history (table already exists)...");
    public string ModelFirstReconciliationSuccess => T("[green]✓[/] Existing table registered. Future Code First migrations will not try to recreate it.");
    public string ModelFirstReconciliationWarn    => T("[yellow]Warning:[/] Could not register the existing table. Before running [blue]database update[/] in the future, manually empty the Up() body in the migration for Add{0}.");

    public string NameParamRequired           => T("The --name <NAME> parameter is required.");
    public string ProjectNameInvalid          => T("The project name contains invalid characters. Use only letters, numbers, '-' and '_'.");
    public string SdkIncompatible             => T("[red]Error:[/] The installed .NET SDK is incompatible with this version of OpenBase.");
    public string SdkUpdateRequired           => T(".NET [blue]{0}[/] or higher is required. Update the SDK at: [blue]https://dot.net[/]");
    public string ApiDatabasePrompt           => T("API database:");
    public string InvalidTypeCombination      => T("[red]Error:[/] The combination Type '[yellow]{0}[/]' + Template '[yellow]{1}[/]' is not valid.");
    public string AvailableCombinations       => T("Available combinations: [blue]--type api --template sqlserver[/]");
    public string CreatingProject             => T("Creating project [blue]{0}[/]...");
    public string CreateProjectFailed         => T("[red]Error:[/] Failed to create the project. Make sure the template is installed with [blue]openbase install[/].");

    public string ProjectConfiguration        => T("[bold]Project configuration[/]");
    public string MediatRLicense              => T("[blue]MediatR[/] license [grey](leave blank if you don't have one)[/]:");
    public string AutoMapperLicense           => T("[blue]AutoMapper[/] license [grey](leave blank if you don't have one)[/]:");
    public string DatabaseServer              => T("Database server:");
    public string DatabaseName                => T("Database name:");
    public string DatabaseUser                => T("Database user:");
    public string DatabasePassword            => T("Database password:");

    public string EntityProperties            => T("[bold]Entity properties[/]");
    public string DatabaseAndTypes            => T("[grey]Database: [blue]{0}[/] | Available types: {1}[/]");
    public string PropertyNamePrompt          => T("[bold]Prop {0}[/] — Name [grey](PascalCase)[/]:");
    public string PropertyTypePrompt          => T("  Type:");
    public string PropertyNotNull             => T("  Not null (required)?");
    public string PropertyAdded               => "  [green]+ {0} ({1})[/]";
    public string AddAnotherProperty          => T("Add another property?");
    public string ColProperty                 => T("Property");
    public string ColType                     => T("Type");
    public string ColPK                       => "PK";
    public string ColNotNull                  => "Not Null";
    public string PropNameRequired            => T("Name is required.");
    public string PropNameMustStartUpper      => T("Must start with an uppercase letter.");
    public string PropNameAlphanumericOnly    => T("Use only letters and numbers.");
    public string PropNameIdReserved          => T("'Id' is reserved as the primary key.");
    public string PropNameAlreadyAdded        => T("Property '{0}' has already been added.");

    public string SchemaOwnerPrompt           => "Schema/owner [[{0}]]:";
    public string TableNamePrompt             => T("Table name:");
    public string TableNameRequired           => T("Please enter the table name.");
    public string ConnectionStringNotFound    => T("[yellow]Connection string not found in appsettings.json.[/]\nEnter the connection string:");
    public string ConnectionStringRequired    => T("The connection string is required.");
    public string ReadingTableStructure       => T("Reading table structure [blue]{0}.{1}[/]...");
    public string ErrorReadingTable           => T("[red]Error reading table:[/] {0}");
    public string NoColumnsFound              => T("[red]No columns found in table [bold]{0}.{1}[/].[/]");
    public string CheckSchemaAndTableName     => T("Check that the schema and table names are correct.");
    public string ColCsType                   => T("C# Type");

    public string SyncingTemplates            => T("[blue]Syncing OpenBase templates...[/]");
    public string PackageStatusVerb           => T("Updating");
    public string PackageSuccessLabel         => T("updated");
    public string PackageErrorLabel           => T("update");
    public string PackageOperationFailed      => T("[red]Error:[/] Failed to {0} [yellow]{1}[/].");
    public string PackageOperationSuccess     => "[green]✓[/] {0} {1}.";
    public string InstallStarting             => T("[blue]Starting OpenBase package installation...[/]");
    public string InstallStatusVerb           => T("Installing");
    public string InstallSuccessLabel         => T("installed");
    public string InstallErrorLabel           => T("install");
    public string UpdatingCli                 => T("Updating OpenBase CLI...");
    public string UpdateCliFailed             => T("[red]Error:[/] Failed to update the OpenBase CLI.");
    public string SomeComponentsUpdateFailed  => T("[yellow]Warning:[/] Some components could not be updated.");
    public string CliUpdated                  => T("[green]✓[/] OpenBase CLI updated.");

    public string UseTypeToSpecify            => T("Use --type to specify the component: cli, sqlserver, postgres, oracle");
    public string InvalidType                 => T("Invalid type '{0}'. Use: cli, sqlserver, postgres, oracle");
    public string RestoringToVersion          => T("[blue]Restoring {0} to version {1}...[/]");
    public string ApplyingVersion             => T("Applying version {0}...");
    public string RestoreFailed               => T("[red]Error:[/] Failed to restore {0} to version {1}.");
    public string RestoreSuccess              => T("[green]✓[/] {0} restored to version {1}.");

    public string InvalidTypeHistory          => T("[red]Error:[/] Invalid type [yellow]{0}[/]. Use: cli, sqlserver, postgres, oracle");
    public string NoHistoryFound              => T("[grey]No update history found.[/]");
    public string ColDate                     => T("[bold]Date[/]");
    public string ColComponent                => T("[bold]Component[/]");
    public string ColPreviousVersion          => T("[bold]Previous Version[/]");
    public string ColNewVersion               => T("[bold]New Version[/]");
    public string ColStatus                   => T("[bold]Status[/]");

    public string CmdRunDescription  => T("Builds and runs the project, opening the browser at Swagger.");
    public string RunNoProjectFound  => T("[red]Error:[/] OpenBase Presentation.Api project not found. Run from the solution root.");
    public string RunStarting        => T("  Starting [blue]{0}[/]...");
    public string RunSwaggerUrl      => T("  Swagger: [blue]{0}[/]");
    public string RunOpeningBrowser  => T("  Opening browser...");
    public string RunStopped         => T("\n[grey]Application stopped.[/]");
    public string HelpRunDesc        => T("Builds and runs the project, opening Swagger in the browser");

    public string CmdBuildDescription  => T("Restores, builds and tests the project.");
    public string BuildNoProjectFound  => T("[red]Error:[/] No .sln or .csproj found in the current directory or its parents.");
    public string BuildRestoring       => T("  Restoring...");
    public string BuildBuilding        => T("  Building...");
    public string BuildTesting         => T("  Testing...");
    public string BuildStepSuccess     => T("  [green]✓[/] done");
    public string BuildStepFailed      => T("  [red]✗[/] failed");
    public string BuildSuccess         => T("\n[green]Build completed successfully.[/]");
    public string BuildFailed          => T("\n[red]Build failed.[/]");
    public string HelpBuildDesc        => T("Restores, builds and tests the project");

    public string HelpSubtitle                => T("[grey]Productivity CLI for Clean Architecture[/]");
    public string HelpColCommand              => T("[bold yellow]Command[/]");
    public string HelpColDescription          => T("[bold yellow]Description[/]");
    public string HelpColExample              => T("[bold yellow]Usage example[/]");
    public string HelpInstallDesc             => T("Installs all OpenBase NuGet templates");
    public string HelpNewDesc                 => T("Creates a new structured project");
    public string HelpScaffoldDesc            => T("Generates all CRUD layers for an entity (Domain, Application, Infra, Presentation)");
    public string HelpHistoryDesc             => T("Shows update history (--type cli | sqlserver | postgres | oracle)");
    public string HelpUpdateDesc              => T("Syncs and updates templates and CLI");
    public string HelpVersionShowDesc         => T("Shows versions of the installed environment");
    public string HelpVersionRestoreDesc      => T("Restores a component to a specific version (--type cli | sqlserver | postgres | oracle)");
    public string HelpExtensionAddDesc        => T("Adds an installable extension to the current project (e.g.: jwt, cache, blob)");
    public string HelpExtensionListDesc       => T("Lists all available extensions and their installation status in the current project");
    public string HelpTip                     => T("[bold white]Tip:[/] Use [blue]--help[/] after any command to see technical details.");
    public string HelpSupport                 => T(" Support ");

    public string ColVersionComponent         => T("[bold]Component[/]");
    public string ColVersion                  => T("[bold]Version[/]");

    public string CmdSpecialistDescription    => T("Adds specialist methods to an existing scaffolded entity.");
    public string HelpSpecialistDesc          => T("Adds specialist Query/Command methods to an existing entity");

    public string CmdInstallDescription       => T("Installs the OpenBase template ecosystem.");
    public string CmdUpdateDescription        => T("Syncs and updates all OpenBase templates.");
    public string CmdNewDescription           => T("Creates a new project based on a template.");
    public string CmdScaffoldDescription      => T("Generates all architecture layers for an entity.");
    public string CmdHistoryDescription       => T("Shows the update history of OpenBase components.");
    public string CmdHelpDescription          => T("Displays help for OpenBase commands.");
    public string CmdVersionDescription       => T("Displays and manages versions of OpenBase components.");
    public string CmdVersionShowDescription   => T("Displays the versions of the OpenBase CLI and template.");
    public string CmdVersionRestoreDescription => T("Restores a component to a specific version.");

    public string ExtensionNoCsprojFound      => T("[red]Error:[/] No .csproj file found in the current directory or its parents.");
    public string ExtensionAlreadyInstalled   => T("[yellow]Warning:[/] Extension [blue]{0}[/] is already installed in this project.");
    public string ExtensionNotFound           => T("[red]Error:[/] Extension [yellow]{0}[/] not found. Run [blue]openbase extension list[/] to see available extensions.");
    public string ExtensionInvalidProvider    => T("[red]Error:[/] Provider [yellow]{0}[/] is not valid for extension [yellow]{1}[/]. Available: [blue]{2}[/].");
    public string ExtensionApplyFailed        => T("[red]Error:[/] Failed to apply extension [yellow]{0}[/]: {1}");
    public string ExtensionAddSuccess         => T("[green]✓[/] Extension [blue]{0}[/] added successfully.");
    public string ExtensionRequiresOpenBaseProject => T("This extension requires an OpenBase Clean Architecture project. Run from the solution root.");
    public string ExtensionAddingPackage      => T("  Adding [blue]{0}[/] to {1}...");
    public string ExtensionPackageAddWarning  => T("  [yellow]Warning:[/] Could not add [yellow]{0}[/]: {1}");
    public string ExtensionAddingReference    => T("  Adding reference [blue]{0}[/] to {1}...");
    public string ExtensionReferenceAddWarning => T("  [yellow]Warning:[/] Could not add reference [yellow]{0}[/]: {1}");
    public string ExtensionFileSkipped        => T("  [yellow]skipped[/] {0} (already exists)");
    public string ExtensionFileCreated        => "  [green]+[/] {0}";
    public string CmdExtensionDescription     => T("Manages installable extensions for an OpenBase project.");
    public string CmdExtensionAddDescription  => T("Adds an extension to the current project.");
    public string CmdExtensionListDescription => T("Lists all available extensions and their installation status.");

    public string ExtensionListColName        => T("[bold]Extension[/]");
    public string ExtensionListColCommand     => T("[bold]Command[/]");
    public string ExtensionListColStatus      => T("[bold]Status[/]");
    public string ExtensionListStatusInstalled => "[green]installed[/]";
    public string ExtensionListStatusAvailable => "[grey]available[/]";

    public string JwtProgramCsInjected        => T("  [green]+[/] Program.cs updated with JWT configuration");
    public string JwtProgramCsAlreadyConfigured => T("  [yellow]skipped[/] Program.cs already has JWT configuration");
    public string JwtProgramCsNotFound        => T("  [yellow]Warning:[/] Program.cs not found — add manually: builder.Services.AddJwtAuthentication(builder.Configuration); app.UseAuthentication(); app.UseAuthorization();");
    public string JwtProgramCsWarning         => T("  [yellow]Warning:[/] Could not modify Program.cs: {0}");
    public string JwtAppSettingsInjected      => T("  [green]+[/] Jwt section added to appsettings.json");
    public string JwtAppSettingsWarning       => T("  [yellow]Warning:[/] Could not modify appsettings.json: {0}");
    public string JwtControllerProtected      => T("  [green]+[/] {0} protected with [[Authorize]]");
    public string JwtSwaggerInjected          => T("  [green]+[/] Swagger configured with JWT Bearer support");
    public string JwtSwaggerWarning           => T("  [yellow]Warning:[/] Could not configure Swagger JWT support: {0}");

    public string HealthChecksProgramCsInjected          => T("  [green]+[/] Program.cs updated with Health Checks configuration");
    public string HealthChecksProgramCsAlreadyConfigured => T("  [yellow]skipped[/] Program.cs already has Health Checks configuration");
    public string HealthChecksProgramCsNotFound          => T("  [yellow]Warning:[/] Program.cs not found — add manually: builder.Services.AddOpenBaseHealthChecks(builder.Configuration); app.MapOpenBaseHealthChecks();");
    public string HealthChecksProgramCsWarning           => T("  [yellow]Warning:[/] Could not modify Program.cs: {0}");

    public string RedisProgramCsInjected          => T("  [green]+[/] Program.cs updated with Redis Cache configuration");
    public string RedisProgramCsAlreadyConfigured => T("  [yellow]skipped[/] Program.cs already has Redis Cache configuration");
    public string RedisProgramCsNotFound          => T("  [yellow]Warning:[/] Program.cs not found — add manually: builder.Services.AddRedisCache(builder.Configuration);");
    public string RedisProgramCsWarning           => T("  [yellow]Warning:[/] Could not modify Program.cs: {0}");
    public string RedisAppSettingsInjected        => T("  [green]+[/] Redis section added to {0}");
    public string RedisAppSettingsWarning         => T("  [yellow]Warning:[/] Could not modify {0}: {1}");

    public string TestingDbConnection    => T("Testing database connection...");
    public string DbConnectionSuccess    => T("[green]✓[/] Database connection established successfully.");
    public string DbConnectionFailed     => T("[yellow]Warning:[/] Could not connect to the database. Check the connection string in appsettings.json.");
    public string ImportFullModelPrompt  => T("Import the full database model (scaffold all tables)?");
    public string ListingTables          => T("Listing tables...");
    public string NoTablesFound          => T("[yellow]No user tables found in the database.[/]");
    public string TablesFound            => T("{0} table(s) found. Enter the entity name for each (PascalCase) or leave blank to skip:");
    public string TableEntityNamePrompt  => T("  [{0}].[{1}] → Entity name:");
    public string TableSkipped           => T("  [grey]skipped[/]");
    public string BulkScaffoldSuccess    => T("\n[green]{0} entity(ies) scaffolded successfully![/]");

    public string SpecialistAddPrompt         => T("Add specialist methods?");
    public string SpecialistMethodNamePrompt  => T("Method name (PascalCase):");
    public string SpecialistTypePrompt        => T("Method type:");
    public string SpecialistQueryChoice       => T("Query — MediatR query (read)");
    public string SpecialistCommandChoice     => T("Command — MediatR command (write)");
    public string SpecialistHttpCallChoice    => T("HTTP call — external call (not implemented)");
    public string SpecialistAddAnother        => T("Add another specialist method?");
    public string SpecialistMethodRequired    => T("The method name is required.");
    public string SpecialistMethodPascalCase  => T("The method name must start with an uppercase letter (PascalCase).");
    public string SpecialistMethodAlphanumeric => T("The method name must contain only letters and numbers.");
    public string SpecialistMethodReserved    => T("'{0}' is reserved by the basic scaffold.");
    public string SpecialistFilesCreated      => T("{0} specialist file(s) created:");
    public string SpecialistSqlPrompt          => T("Enter the SQL/operation with parameters as {{ paramName }}:");
    public string SpecialistParamsDetected    => T("Parameters detected: {0}");
    public string SpecialistParamTypePrompt   => T("  {0} — C# type:");
    public string SpecialistPaginatedPrompt   => T("Does the query return paginated results?");
    public string SpecialistResultColumnsTitle => T("Define the result columns:");
    public string SpecialistResultColumnName  => T("  Column {0} name (blank to finish):");
    public string SpecialistResultColumnTypePrompt => T("  {0} — C# type:");

    public string ScaffoldUpdateEntityNotFound  => T("[red]Error:[/] Entity [blue]{0}[/] not found. Run [blue]openbase scaffold --entity {0}[/] first.");
    public string ScaffoldUpdateNoChanges       => T("[green]✓[/] No differences detected. Files are already up to date.");
    public string ScaffoldUpdateDiffTitle       => T("\nDetected differences:");
    public string ScaffoldUpdateUncommittedWarn => T("[yellow]⚠️  The following files have uncommitted changes:[/]");
    public string ScaffoldUpdateUncommittedHint => T("[grey]Consider committing or stashing before continuing.[/]");
    public string ScaffoldUpdateApplyChanges    => T("Apply changes?");
    public string ScaffoldUpdateDestructiveWarn => T("[yellow]⚠️  {0} property(ies) will be REMOVED from multiple files.[/]");
    public string ScaffoldUpdateConfirmRemoval  => T("Confirm removal?");
    public string FilesUpdated                  => T("{0} file(s) updated:");
    public string ScaffoldUpdateSuccess         => T("\n[green]Scaffold for entity [bold]{0}[/] updated successfully![/]");
    public string ScaffoldUpdateMigrationHint   => T("[grey]Don't forget to create a migration: [blue]dotnet ef migrations add Update{0}[/][/]");
}


public sealed class EnStrings() : BaseStrings(new Dictionary<string, string>()) { }


public sealed class PtBrStrings() : BaseStrings(new Dictionary<string, string>
{
    ["EntityParamRequired"]          = "O parâmetro --entity <ENTIDADE> é obrigatório.",
    ["EntityMustBePascalCase"]       = "O nome da entidade deve começar com letra maiúscula (PascalCase).",
    ["EntityMustBeAlphanumeric"]     = "O nome da entidade deve conter apenas letras e números.",
    ["ProjectStructureNotFound"]     = "[red]Erro:[/] Estrutura de projeto OpenBase não encontrada.",
    ["RunInProjectRoot"]             = "Execute este comando na raiz de um projeto criado com [blue]openbase new[/].",
    ["OrProvideNamespace"]           = "Ou informe o namespace com [blue]--namespace <NAMESPACE>[/].",
    ["ProceedWithScaffold"]          = "Prosseguir com o scaffold?",
    ["GeneratingScaffold"]           = "Gerando scaffold para [blue]{0}[/]...",
    ["FilesCreated"]                 = "{0} arquivo(s) criado(s):",
    ["FilesSkipped"]                 = "{0} arquivo(s) já existente(s) ignorado(s):",
    ["FilesErrors"]                  = "{0} erro(s):",
    ["ScaffoldSuccess"]              = "\n[green]Scaffold da entidade [bold]{0}[/] gerado com sucesso![/]",
    ["NextSteps"]                    = "Próximos passos:",
    ["AddDbSet"]                     = "  1. Adicione [blue]DbSet<{0}> {1} {{ get; set; }}[/] ao DbContext",
    ["RunMigrationsAdd"]             = "  2. Execute [blue]dotnet ef migrations add Add{0}[/]",
    ["RunDatabaseUpdate"]            = "  3. Execute [blue]dotnet ef database update[/]",
    ["RestoringNuGetPackages"]       = "Restaurando pacotes NuGet...",
    ["RestorePackagesWarning"]       = "[yellow]Aviso:[/] Falha ao restaurar pacotes. Tentando gerar a migration mesmo assim...",
    ["GeneratingMigration"]          = "Gerando migration [blue]Add{0}[/]...",
    ["MigrationFailed"]              = "[red]Erro:[/] Falha ao gerar a migration.",
    ["RunMigrationManually"]         = "Execute manualmente: [blue]dotnet ef migrations add Add{0}[/]",
    ["MigrationGenerated"]           = "[green]Migration Add{0} gerada.[/]",
    ["RunDatabaseUpdateNow"]         = "Executar [blue]database update[/] agora?",
    ["ExecutingDatabaseUpdate"]      = "Executando [blue]database update[/]...",
    ["DatabaseUpdateFailed"]         = "[red]Erro:[/] Falha ao executar database update.",
    ["DatabaseUpdatedSuccess"]       = "[green]Banco de dados atualizado com sucesso.[/]",
    ["GeneratingUpdateMigration"]    = "Gerando migration [blue]Update{0}[/]...",
    ["UpdateMigrationGenerated"]     = "[green]Migration Update{0} gerada.[/]",
    ["RunUpdateMigrationManually"]   = "Execute manualmente: [blue]dotnet ef migrations add Update{0}[/]",
    ["HowToGenerateScaffold"]        = "\nComo deseja gerar o scaffold?",
    ["CodeFirstChoice"]              = "Code First (definir propriedades manualmente)",
    ["ModelFirstChoice"]             = "Model First (ler estrutura de uma tabela existente)",
    ["ModelFirstReconciliationInfo"] = "Registrando [blue]Add{0}[/] no histórico de migrations do EF (tabela já existe)...",
    ["ModelFirstReconciliationSuccess"] = "[green]✓[/] Tabela existente registrada. Migrations futuras do Code First não tentarão recriá-la.",
    ["ModelFirstReconciliationWarn"] = "[yellow]Aviso:[/] Não foi possível registrar a tabela existente. Antes de executar [blue]database update[/] no futuro, esvazie manualmente o corpo do Up() na migration Add{0}.",
    ["NameParamRequired"]            = "O parâmetro --name <NOME> é obrigatório.",
    ["ProjectNameInvalid"]           = "O nome do projeto contém caracteres inválidos. Use apenas letras, números, '-' e '_'.",
    ["SdkIncompatible"]              = "[red]Erro:[/] O .NET SDK instalado é incompatível com esta versão do OpenBase.",
    ["SdkUpdateRequired"]            = "É necessário o [blue].NET {0}[/] ou superior. Atualize o SDK em: [blue]https://dot.net[/]",
    ["ApiDatabasePrompt"]            = "Banco de dados da API:",
    ["InvalidTypeCombination"]       = "[red]Erro:[/] A combinação Tipo '[yellow]{0}[/]' + Template '[yellow]{1}[/]' não é válida.",
    ["AvailableCombinations"]        = "Combinações disponíveis: [blue]--type api --template sqlserver[/]",
    ["CreatingProject"]              = "Criando projeto [blue]{0}[/]...",
    ["CreateProjectFailed"]          = "[red]Erro:[/] Falha ao criar o projeto. Verifique se o template está instalado com [blue]openbase install[/].",
    ["ProjectConfiguration"]         = "[bold]Configuração do projeto[/]",
    ["MediatRLicense"]               = "Licença do [blue]MediatR[/] [grey](deixe em branco se não tiver)[/]:",
    ["AutoMapperLicense"]            = "Licença do [blue]AutoMapper[/] [grey](deixe em branco se não tiver)[/]:",
    ["DatabaseServer"]               = "Servidor do banco de dados:",
    ["DatabaseName"]                 = "Nome do banco de dados:",
    ["DatabaseUser"]                 = "Usuário do banco de dados:",
    ["DatabasePassword"]             = "Senha do banco de dados:",
    ["EntityProperties"]             = "[bold]Propriedades da entidade[/]",
    ["DatabaseAndTypes"]             = "[grey]Banco: [blue]{0}[/] | Tipos disponíveis: {1}[/]",
    ["PropertyNamePrompt"]           = "[bold]Prop {0}[/] — Nome [grey](PascalCase)[/]:",
    ["PropertyTypePrompt"]           = "  Tipo:",
    ["PropertyNotNull"]              = "  Not null (obrigatório)?",
    ["AddAnotherProperty"]           = "Adicionar outra propriedade?",
    ["ColProperty"]                  = "Propriedade",
    ["ColType"]                      = "Tipo",
    ["PropNameRequired"]             = "O nome é obrigatório.",
    ["PropNameMustStartUpper"]       = "Deve começar com letra maiúscula.",
    ["PropNameAlphanumericOnly"]     = "Use apenas letras e números.",
    ["PropNameIdReserved"]           = "'Id' é reservado como chave primária.",
    ["PropNameAlreadyAdded"]         = "Propriedade '{0}' já foi adicionada.",
    ["TableNamePrompt"]              = "Nome da tabela:",
    ["TableNameRequired"]            = "Informe o nome da tabela.",
    ["ConnectionStringNotFound"]     = "[yellow]Connection string não encontrada no appsettings.json.[/]\nInforme a connection string:",
    ["ConnectionStringRequired"]     = "A connection string é obrigatória.",
    ["ReadingTableStructure"]        = "Lendo estrutura da tabela [blue]{0}.{1}[/]...",
    ["ErrorReadingTable"]            = "[red]Erro ao ler a tabela:[/] {0}",
    ["NoColumnsFound"]               = "[red]Nenhuma coluna encontrada na tabela [bold]{0}.{1}[/].[/]",
    ["CheckSchemaAndTableName"]      = "Verifique se o nome do schema e da tabela estão corretos.",
    ["ColCsType"]                    = "Tipo C#",
    ["SyncingTemplates"]             = "[blue]Sincronizando templates OpenBase...[/]",
    ["PackageStatusVerb"]            = "Atualizando",
    ["PackageSuccessLabel"]          = "atualizado",
    ["PackageErrorLabel"]            = "atualizar",
    ["PackageOperationFailed"]       = "[red]Erro:[/] Falha ao {0} [yellow]{1}[/].",
    ["InstallStarting"]              = "[blue]Iniciando a instalação dos pacotes OpenBase...[/]",
    ["InstallStatusVerb"]            = "Instalando",
    ["InstallSuccessLabel"]          = "instalado",
    ["InstallErrorLabel"]            = "instalar",
    ["UpdatingCli"]                  = "Atualizando OpenBase CLI...",
    ["UpdateCliFailed"]              = "[red]Erro:[/] Falha ao atualizar a OpenBase CLI.",
    ["SomeComponentsUpdateFailed"]   = "[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.",
    ["CliUpdated"]                   = "[green]✓[/] OpenBase CLI atualizada.",
    ["UseTypeToSpecify"]             = "Use --type para especificar o componente: cli, sqlserver, postgres, oracle",
    ["InvalidType"]                  = "Tipo inválido '{0}'. Use: cli, sqlserver, postgres, oracle",
    ["RestoringToVersion"]           = "[blue]Restaurando {0} para a versão {1}...[/]",
    ["ApplyingVersion"]              = "Aplicando versão {0}...",
    ["RestoreFailed"]                = "[red]Erro:[/] Falha ao restaurar {0} para a versão {1}.",
    ["RestoreSuccess"]               = "[green]✓[/] {0} restaurado para a versão {1}.",
    ["InvalidTypeHistory"]           = "[red]Erro:[/] Tipo inválido [yellow]{0}[/]. Use: cli, sqlserver, postgres, oracle",
    ["NoHistoryFound"]               = "[grey]Nenhum histórico de atualização encontrado.[/]",
    ["ColDate"]                      = "[bold]Data[/]",
    ["ColComponent"]                 = "[bold]Componente[/]",
    ["ColPreviousVersion"]           = "[bold]Versão Anterior[/]",
    ["ColNewVersion"]                = "[bold]Nova Versão[/]",
    ["HelpSubtitle"]                 = "[grey]CLI de produtividade para Arquitetura Limpa[/]",
    ["HelpColCommand"]               = "[bold yellow]Comando[/]",
    ["HelpColDescription"]           = "[bold yellow]Descrição[/]",
    ["HelpColExample"]               = "[bold yellow]Exemplo de uso[/]",
    ["HelpInstallDesc"]              = "Instala todos os templates NuGet do OpenBase",
    ["HelpNewDesc"]                  = "Cria um novo projeto estruturado",
    ["HelpScaffoldDesc"]             = "Gera todas as camadas CRUD de uma entidade (Domain, Application, Infra, Presentation)",
    ["HelpHistoryDesc"]              = "Exibe o histórico de atualizações (--type cli | sqlserver | postgres | oracle)",
    ["HelpUpdateDesc"]               = "Sincroniza e atualiza templates e a CLI",
    ["HelpVersionShowDesc"]          = "Mostra versões do ambiente instalado",
    ["HelpVersionRestoreDesc"]       = "Restaura um componente para uma versão específica (--type cli | sqlserver | postgres | oracle)",
    ["HelpExtensionAddDesc"]         = "Adiciona uma extensão instalável ao projeto atual (ex: jwt, cache, blob)",
    ["HelpExtensionListDesc"]        = "Lista todas as extensões disponíveis e o status de instalação no projeto atual",
    ["HelpTip"]                      = "[bold white]Dica:[/] Use [blue]--help[/] após qualquer comando para ver detalhes técnicos.",
    ["HelpSupport"]                  = " Suporte ",
    ["ColVersionComponent"]          = "[bold]Componente[/]",
    ["ColVersion"]                   = "[bold]Versão[/]",
    ["CmdSpecialistDescription"]      = "Adiciona métodos especialistas a um scaffold de entidade existente.",
    ["HelpSpecialistDesc"]           = "Adiciona métodos especialistas (Query/Command) a uma entidade já scaffolded",
    ["CmdInstallDescription"]        = "Instala o ecossistema de templates OpenBase.",
    ["CmdUpdateDescription"]         = "Sincroniza e atualiza todos os templates OpenBase.",
    ["CmdNewDescription"]            = "Cria um novo projeto baseado em um template.",
    ["CmdScaffoldDescription"]       = "Gera todas as camadas da arquitetura para uma entidade.",
    ["CmdHistoryDescription"]        = "Exibe o histórico de atualizações dos componentes OpenBase.",
    ["CmdHelpDescription"]           = "Exibe a ajuda para os comandos do OpenBase.",
    ["CmdVersionDescription"]        = "Exibe e gerencia versões dos componentes OpenBase.",
    ["CmdVersionShowDescription"]    = "Exibe as versões da CLI e do template do OpenBase.",
    ["CmdVersionRestoreDescription"] = "Restaura um componente para uma versão específica.",
    ["ExtensionNoCsprojFound"]       = "[red]Erro:[/] Nenhum arquivo .csproj encontrado no diretório atual ou em seus pais.",
    ["ExtensionAlreadyInstalled"]    = "[yellow]Aviso:[/] A extensão [blue]{0}[/] já está instalada neste projeto.",
    ["ExtensionNotFound"]            = "[red]Erro:[/] Extensão [yellow]{0}[/] não encontrada. Execute [blue]openbase extension list[/] para ver as extensões disponíveis.",
    ["ExtensionInvalidProvider"]     = "[red]Erro:[/] O provider [yellow]{0}[/] não é válido para a extensão [yellow]{1}[/]. Disponíveis: [blue]{2}[/].",
    ["ExtensionApplyFailed"]         = "[red]Erro:[/] Falha ao aplicar a extensão [yellow]{0}[/]: {1}",
    ["ExtensionAddSuccess"]          = "[green]✓[/] Extensão [blue]{0}[/] adicionada com sucesso.",
    ["ExtensionRequiresOpenBaseProject"] = "Esta extensão requer um projeto OpenBase com Arquitetura Limpa. Execute na raiz da solution.",
    ["ExtensionAddingPackage"]       = "  Adicionando [blue]{0}[/] ao {1}...",
    ["ExtensionPackageAddWarning"]   = "  [yellow]Aviso:[/] Não foi possível adicionar [yellow]{0}[/]: {1}",
    ["ExtensionAddingReference"]     = "  Adicionando referência [blue]{0}[/] ao {1}...",
    ["ExtensionReferenceAddWarning"] = "  [yellow]Aviso:[/] Não foi possível adicionar a referência [yellow]{0}[/]: {1}",
    ["ExtensionFileSkipped"]         = "  [yellow]ignorado[/] {0} (já existe)",
    ["CmdExtensionDescription"]      = "Gerencia extensões instaláveis para um projeto OpenBase.",
    ["CmdExtensionAddDescription"]   = "Adiciona uma extensão ao projeto atual.",
    ["CmdExtensionListDescription"]  = "Lista todas as extensões disponíveis e seu status de instalação.",
    ["ExtensionListColName"]         = "[bold]Extensão[/]",
    ["ExtensionListColCommand"]      = "[bold]Comando[/]",
    ["ExtensionListColStatus"]       = "[bold]Status[/]",
    ["ExtensionListStatusInstalled"] = "[green]instalada[/]",
    ["ExtensionListStatusAvailable"] = "[grey]disponível[/]",
    ["JwtProgramCsInjected"]         = "  [green]+[/] Program.cs atualizado com configuração JWT",
    ["JwtProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs já possui configuração JWT",
    ["JwtProgramCsNotFound"]         = "  [yellow]Aviso:[/] Program.cs não encontrado — adicione manualmente: builder.Services.AddJwtAuthentication(builder.Configuration); app.UseAuthentication(); app.UseAuthorization();",
    ["JwtProgramCsWarning"]          = "  [yellow]Aviso:[/] Não foi possível modificar Program.cs: {0}",
    ["JwtAppSettingsInjected"]       = "  [green]+[/] Seção Jwt adicionada ao appsettings.json",
    ["JwtAppSettingsWarning"]        = "  [yellow]Aviso:[/] Não foi possível modificar appsettings.json: {0}",
    ["JwtControllerProtected"]       = "  [green]+[/] {0} protegida com [[Authorize]]",
    ["JwtSwaggerInjected"]           = "  [green]+[/] Swagger configurado com suporte a JWT Bearer",
    ["JwtSwaggerWarning"]            = "  [yellow]Aviso:[/] Não foi possível configurar o Swagger para JWT: {0}",
    ["CmdRunDescription"]  = "Compila e executa o projeto, abrindo o browser no Swagger.",
    ["RunNoProjectFound"]  = "[red]Erro:[/] Projeto Presentation.Api do OpenBase não encontrado. Execute na raiz da solution.",
    ["RunStarting"]        = "  Iniciando [blue]{0}[/]...",
    ["RunSwaggerUrl"]      = "  Swagger: [blue]{0}[/]",
    ["RunOpeningBrowser"]  = "  Abrindo o browser...",
    ["RunStopped"]         = "\n[grey]Aplicação encerrada.[/]",
    ["HelpRunDesc"]        = "Compila e executa o projeto, abrindo o Swagger no browser",
    ["CmdBuildDescription"]  = "Restaura, compila e testa o projeto.",
    ["BuildNoProjectFound"]  = "[red]Erro:[/] Nenhum .sln ou .csproj encontrado no diretório atual ou em seus pais.",
    ["BuildRestoring"]       = "  Restaurando...",
    ["BuildBuilding"]        = "  Compilando...",
    ["BuildTesting"]         = "  Testando...",
    ["BuildStepSuccess"]     = "  [green]✓[/] concluído",
    ["BuildStepFailed"]      = "  [red]✗[/] falhou",
    ["BuildSuccess"]         = "\n[green]Build concluído com sucesso.[/]",
    ["BuildFailed"]          = "\n[red]Build falhou.[/]",
    ["HelpBuildDesc"]        = "Restaura, compila e testa o projeto",
    ["HealthChecksProgramCsInjected"]          = "  [green]+[/] Program.cs atualizado com configuração de Health Checks",
    ["HealthChecksProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs já possui configuração de Health Checks",
    ["HealthChecksProgramCsNotFound"]          = "  [yellow]Aviso:[/] Program.cs não encontrado — adicione manualmente: builder.Services.AddOpenBaseHealthChecks(builder.Configuration); app.MapOpenBaseHealthChecks();",
    ["HealthChecksProgramCsWarning"]           = "  [yellow]Aviso:[/] Não foi possível modificar Program.cs: {0}",
    ["RedisProgramCsInjected"]          = "  [green]+[/] Program.cs atualizado com configuração do Redis Cache",
    ["RedisProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs já possui configuração do Redis Cache",
    ["RedisProgramCsNotFound"]          = "  [yellow]Aviso:[/] Program.cs não encontrado — adicione manualmente: builder.Services.AddRedisCache(builder.Configuration);",
    ["RedisProgramCsWarning"]           = "  [yellow]Aviso:[/] Não foi possível modificar Program.cs: {0}",
    ["RedisAppSettingsInjected"]        = "  [green]+[/] Seção Redis adicionada ao {0}",
    ["RedisAppSettingsWarning"]         = "  [yellow]Aviso:[/] Não foi possível modificar {0}: {1}",
    ["TestingDbConnection"]    = "Testando conexão com o banco de dados...",
    ["DbConnectionSuccess"]    = "[green]✓[/] Conexão com o banco de dados estabelecida com sucesso.",
    ["DbConnectionFailed"]     = "[yellow]Aviso:[/] Não foi possível conectar ao banco de dados. Verifique a connection string no appsettings.json.",
    ["ImportFullModelPrompt"]  = "Importar todo o modelo do banco de dados (scaffold de todas as tabelas)?",
    ["ListingTables"]          = "Listando tabelas...",
    ["NoTablesFound"]          = "[yellow]Nenhuma tabela de usuário encontrada no banco de dados.[/]",
    ["TablesFound"]            = "{0} tabela(s) encontrada(s). Informe o nome da entidade para cada uma (PascalCase) ou deixe em branco para pular:",
    ["TableEntityNamePrompt"]  = "  [{0}].[{1}] → Nome da entidade:",
    ["TableSkipped"]           = "  [grey]ignorada[/]",
    ["BulkScaffoldSuccess"]    = "\n[green]{0} entidade(s) gerada(s) com sucesso![/]",
    ["SpecialistAddPrompt"]         = "Deseja adicionar métodos especialistas?",
    ["SpecialistMethodNamePrompt"]  = "Nome do método (PascalCase):",
    ["SpecialistTypePrompt"]        = "Tipo do método:",
    ["SpecialistQueryChoice"]       = "Query — consulta MediatR (leitura)",
    ["SpecialistCommandChoice"]     = "Command — comando MediatR (escrita)",
    ["SpecialistHttpCallChoice"]    = "HTTP call — chamada externa (não implementada)",
    ["SpecialistAddAnother"]        = "Adicionar outro método especialista?",
    ["SpecialistMethodRequired"]    = "O nome do método é obrigatório.",
    ["SpecialistMethodPascalCase"]  = "O nome do método deve começar com letra maiúscula (PascalCase).",
    ["SpecialistMethodAlphanumeric"] = "O nome do método deve conter apenas letras e números.",
    ["SpecialistMethodReserved"]    = "'{0}' é reservado pelo scaffold básico.",
    ["SpecialistFilesCreated"]      = "{0} arquivo(s) especialista(s) criado(s):",
    ["SpecialistSqlPrompt"]              = "Informe o SQL/operação com parâmetros no formato {{ nomeParam }}:",
    ["SpecialistParamsDetected"]         = "Parâmetros detectados: {0}",
    ["SpecialistParamTypePrompt"]        = "  {0} — tipo C#:",
    ["SpecialistPaginatedPrompt"]        = "A query retorna resultado paginado?",
    ["SpecialistResultColumnsTitle"]     = "Defina as colunas do resultado:",
    ["SpecialistResultColumnName"]       = "  Nome da coluna {0} (Enter para finalizar):",
    ["SpecialistResultColumnTypePrompt"] = "  {0} — tipo C#:",
    ["ScaffoldUpdateEntityNotFound"]  = "[red]Erro:[/] Entidade [blue]{0}[/] não encontrada. Execute [blue]openbase scaffold --entity {0}[/] primeiro.",
    ["ScaffoldUpdateNoChanges"]       = "[green]✓[/] Nenhuma diferença detectada. Os arquivos já estão atualizados.",
    ["ScaffoldUpdateDiffTitle"]       = "\nDiferenças detectadas:",
    ["ScaffoldUpdateUncommittedWarn"] = "[yellow]⚠️  Os seguintes arquivos têm alterações não commitadas:[/]",
    ["ScaffoldUpdateUncommittedHint"] = "[grey]Considere fazer commit ou stash antes de continuar.[/]",
    ["ScaffoldUpdateApplyChanges"]    = "Aplicar mudanças?",
    ["ScaffoldUpdateDestructiveWarn"] = "[yellow]⚠️  {0} propriedade(s) serão REMOVIDAS de múltiplos arquivos.[/]",
    ["ScaffoldUpdateConfirmRemoval"]  = "Confirmar remoção?",
    ["FilesUpdated"]                  = "{0} arquivo(s) atualizado(s):",
    ["ScaffoldUpdateSuccess"]         = "\n[green]Scaffold da entidade [bold]{0}[/] atualizado com sucesso![/]",
    ["ScaffoldUpdateMigrationHint"]   = "[grey]Não esqueça de criar a migration: [blue]dotnet ef migrations add Update{0}[/][/]",
}) { }


public sealed class EsStrings() : BaseStrings(new Dictionary<string, string>
{
    ["EntityParamRequired"]          = "El parámetro --entity <ENTIDAD> es obligatorio.",
    ["EntityMustBePascalCase"]       = "El nombre de la entidad debe comenzar con mayúscula (PascalCase).",
    ["EntityMustBeAlphanumeric"]     = "El nombre de la entidad debe contener solo letras y números.",
    ["ProjectStructureNotFound"]     = "[red]Error:[/] No se encontró la estructura de proyecto OpenBase.",
    ["RunInProjectRoot"]             = "Ejecute este comando en la raíz de un proyecto creado con [blue]openbase new[/].",
    ["OrProvideNamespace"]           = "O indique el namespace con [blue]--namespace <NAMESPACE>[/].",
    ["ProceedWithScaffold"]          = "¿Continuar con el scaffold?",
    ["GeneratingScaffold"]           = "Generando scaffold para [blue]{0}[/]...",
    ["FilesCreated"]                 = "{0} archivo(s) creado(s):",
    ["FilesSkipped"]                 = "{0} archivo(s) ya existente(s), omitido(s):",
    ["FilesErrors"]                  = "{0} error(es):",
    ["ScaffoldSuccess"]              = "\n[green]Scaffold de la entidad [bold]{0}[/] generado con éxito![/]",
    ["NextSteps"]                    = "Próximos pasos:",
    ["AddDbSet"]                     = "  1. Agregue [blue]DbSet<{0}> {1} {{ get; set; }}[/] al DbContext",
    ["RunMigrationsAdd"]             = "  2. Ejecute [blue]dotnet ef migrations add Add{0}[/]",
    ["RunDatabaseUpdate"]            = "  3. Ejecute [blue]dotnet ef database update[/]",
    ["RestoringNuGetPackages"]       = "Restaurando paquetes NuGet...",
    ["RestorePackagesWarning"]       = "[yellow]Aviso:[/] Error al restaurar paquetes. Intentando generar la migración de todos modos...",
    ["GeneratingMigration"]          = "Generando migración [blue]Add{0}[/]...",
    ["MigrationFailed"]              = "[red]Error:[/] Error al generar la migración.",
    ["RunMigrationManually"]         = "Ejecute manualmente: [blue]dotnet ef migrations add Add{0}[/]",
    ["MigrationGenerated"]           = "[green]Migración Add{0} generada.[/]",
    ["RunDatabaseUpdateNow"]         = "¿Ejecutar [blue]database update[/] ahora?",
    ["ExecutingDatabaseUpdate"]      = "Ejecutando [blue]database update[/]...",
    ["DatabaseUpdateFailed"]         = "[red]Error:[/] Error al ejecutar database update.",
    ["DatabaseUpdatedSuccess"]       = "[green]Base de datos actualizada con éxito.[/]",
    ["GeneratingUpdateMigration"]    = "Generando migración [blue]Update{0}[/]...",
    ["UpdateMigrationGenerated"]     = "[green]Migración Update{0} generada.[/]",
    ["RunUpdateMigrationManually"]   = "Ejecute manualmente: [blue]dotnet ef migrations add Update{0}[/]",
    ["HowToGenerateScaffold"]        = "\n¿Cómo desea generar el scaffold?",
    ["CodeFirstChoice"]              = "Code First (definir propiedades manualmente)",
    ["ModelFirstChoice"]             = "Model First (leer estructura de una tabla existente)",
    ["ModelFirstReconciliationInfo"] = "Registrando [blue]Add{0}[/] en el historial de migraciones de EF (la tabla ya existe)...",
    ["ModelFirstReconciliationSuccess"] = "[green]✓[/] Tabla existente registrada. Las migraciones futuras de Code First no intentarán recrearla.",
    ["ModelFirstReconciliationWarn"] = "[yellow]Aviso:[/] No se pudo registrar la tabla existente. Antes de ejecutar [blue]database update[/] en el futuro, vacíe manualmente el cuerpo de Up() en la migración Add{0}.",
    ["NameParamRequired"]            = "El parámetro --name <NOMBRE> es obligatorio.",
    ["ProjectNameInvalid"]           = "El nombre del proyecto contiene caracteres no válidos. Use solo letras, números, '-' y '_'.",
    ["SdkIncompatible"]              = "[red]Error:[/] El .NET SDK instalado es incompatible con esta versión de OpenBase.",
    ["SdkUpdateRequired"]            = "Se requiere [blue].NET {0}[/] o superior. Actualice el SDK en: [blue]https://dot.net[/]",
    ["ApiDatabasePrompt"]            = "Base de datos de la API:",
    ["InvalidTypeCombination"]       = "[red]Error:[/] La combinación Tipo '[yellow]{0}[/]' + Template '[yellow]{1}[/]' no es válida.",
    ["AvailableCombinations"]        = "Combinaciones disponibles: [blue]--type api --template sqlserver[/]",
    ["CreatingProject"]              = "Creando proyecto [blue]{0}[/]...",
    ["CreateProjectFailed"]          = "[red]Error:[/] Error al crear el proyecto. Verifique que el template esté instalado con [blue]openbase install[/].",
    ["ProjectConfiguration"]         = "[bold]Configuración del proyecto[/]",
    ["MediatRLicense"]               = "Licencia de [blue]MediatR[/] [grey](deje en blanco si no tiene)[/]:",
    ["AutoMapperLicense"]            = "Licencia de [blue]AutoMapper[/] [grey](deje en blanco si no tiene)[/]:",
    ["DatabaseServer"]               = "Servidor de base de datos:",
    ["DatabaseName"]                 = "Nombre de la base de datos:",
    ["DatabaseUser"]                 = "Usuario de la base de datos:",
    ["DatabasePassword"]             = "Contraseña de la base de datos:",
    ["EntityProperties"]             = "[bold]Propiedades de la entidad[/]",
    ["DatabaseAndTypes"]             = "[grey]Base de datos: [blue]{0}[/] | Tipos disponibles: {1}[/]",
    ["PropertyNamePrompt"]           = "[bold]Prop {0}[/] — Nombre [grey](PascalCase)[/]:",
    ["PropertyTypePrompt"]           = "  Tipo:",
    ["PropertyNotNull"]              = "  ¿Not null (obligatorio)?",
    ["AddAnotherProperty"]           = "¿Agregar otra propiedad?",
    ["ColProperty"]                  = "Propiedad",
    ["ColType"]                      = "Tipo",
    ["PropNameRequired"]             = "El nombre es obligatorio.",
    ["PropNameMustStartUpper"]       = "Debe comenzar con letra mayúscula.",
    ["PropNameAlphanumericOnly"]     = "Use solo letras y números.",
    ["PropNameIdReserved"]           = "'Id' está reservado como clave primaria.",
    ["PropNameAlreadyAdded"]         = "La propiedad '{0}' ya fue agregada.",
    ["TableNamePrompt"]              = "Nombre de la tabla:",
    ["TableNameRequired"]            = "Ingrese el nombre de la tabla.",
    ["ConnectionStringNotFound"]     = "[yellow]Connection string no encontrada en appsettings.json.[/]\nIngrese la connection string:",
    ["ConnectionStringRequired"]     = "La connection string es obligatoria.",
    ["ReadingTableStructure"]        = "Leyendo estructura de la tabla [blue]{0}.{1}[/]...",
    ["ErrorReadingTable"]            = "[red]Error al leer la tabla:[/] {0}",
    ["NoColumnsFound"]               = "[red]No se encontraron columnas en la tabla [bold]{0}.{1}[/].[/]",
    ["CheckSchemaAndTableName"]      = "Verifique que el nombre del schema y de la tabla sean correctos.",
    ["ColCsType"]                    = "Tipo C#",
    ["SyncingTemplates"]             = "[blue]Sincronizando templates OpenBase...[/]",
    ["PackageStatusVerb"]            = "Actualizando",
    ["PackageSuccessLabel"]          = "actualizado",
    ["PackageErrorLabel"]            = "actualizar",
    ["PackageOperationFailed"]       = "[red]Error:[/] Error al {0} [yellow]{1}[/].",
    ["InstallStarting"]              = "[blue]Iniciando la instalación de los paquetes OpenBase...[/]",
    ["InstallStatusVerb"]            = "Instalando",
    ["InstallSuccessLabel"]          = "instalado",
    ["InstallErrorLabel"]            = "instalar",
    ["UpdatingCli"]                  = "Actualizando OpenBase CLI...",
    ["UpdateCliFailed"]              = "[red]Error:[/] Error al actualizar la OpenBase CLI.",
    ["SomeComponentsUpdateFailed"]   = "[yellow]Aviso:[/] Algunos componentes no pudieron actualizarse.",
    ["CliUpdated"]                   = "[green]✓[/] OpenBase CLI actualizada.",
    ["UseTypeToSpecify"]             = "Use --type para especificar el componente: cli, sqlserver, postgres, oracle",
    ["InvalidType"]                  = "Tipo inválido '{0}'. Use: cli, sqlserver, postgres, oracle",
    ["RestoringToVersion"]           = "[blue]Restaurando {0} a la versión {1}...[/]",
    ["ApplyingVersion"]              = "Aplicando versión {0}...",
    ["RestoreFailed"]                = "[red]Error:[/] Error al restaurar {0} a la versión {1}.",
    ["RestoreSuccess"]               = "[green]✓[/] {0} restaurado a la versión {1}.",
    ["InvalidTypeHistory"]           = "[red]Error:[/] Tipo inválido [yellow]{0}[/]. Use: cli, sqlserver, postgres, oracle",
    ["NoHistoryFound"]               = "[grey]No se encontró historial de actualizaciones.[/]",
    ["ColDate"]                      = "[bold]Fecha[/]",
    ["ColComponent"]                 = "[bold]Componente[/]",
    ["ColPreviousVersion"]           = "[bold]Versión Anterior[/]",
    ["ColNewVersion"]                = "[bold]Nueva Versión[/]",
    ["ColStatus"]                    = "[bold]Estado[/]",
    ["HelpSubtitle"]                 = "[grey]CLI de productividad para Arquitectura Limpia[/]",
    ["HelpColCommand"]               = "[bold yellow]Comando[/]",
    ["HelpColDescription"]           = "[bold yellow]Descripción[/]",
    ["HelpColExample"]               = "[bold yellow]Ejemplo de uso[/]",
    ["HelpInstallDesc"]              = "Instala todos los templates NuGet de OpenBase",
    ["HelpNewDesc"]                  = "Crea un nuevo proyecto estructurado",
    ["HelpScaffoldDesc"]             = "Genera todas las capas CRUD de una entidad (Domain, Application, Infra, Presentation)",
    ["HelpHistoryDesc"]              = "Muestra el historial de actualizaciones (--type cli | sqlserver | postgres | oracle)",
    ["HelpUpdateDesc"]               = "Sincroniza y actualiza templates y la CLI",
    ["HelpVersionShowDesc"]          = "Muestra versiones del entorno instalado",
    ["HelpVersionRestoreDesc"]       = "Restaura un componente a una versión específica (--type cli | sqlserver | postgres | oracle)",
    ["HelpExtensionAddDesc"]         = "Agrega una extensión instalable al proyecto actual (ej: jwt, cache, blob)",
    ["HelpExtensionListDesc"]        = "Lista todas las extensiones disponibles y el estado de instalación en el proyecto actual",
    ["HelpTip"]                      = "[bold white]Consejo:[/] Use [blue]--help[/] después de cualquier comando para ver detalles técnicos.",
    ["HelpSupport"]                  = " Soporte ",
    ["ColVersionComponent"]          = "[bold]Componente[/]",
    ["ColVersion"]                   = "[bold]Versión[/]",
    ["CmdSpecialistDescription"]      = "Agrega métodos especialistas a un scaffold de entidad existente.",
    ["HelpSpecialistDesc"]           = "Agrega métodos especialistas (Query/Command) a una entidad ya scaffolded",
    ["CmdInstallDescription"]        = "Instala el ecosistema de templates OpenBase.",
    ["CmdUpdateDescription"]         = "Sincroniza y actualiza todos los templates OpenBase.",
    ["CmdNewDescription"]            = "Crea un nuevo proyecto basado en un template.",
    ["CmdScaffoldDescription"]       = "Genera todas las capas de arquitectura para una entidad.",
    ["CmdHistoryDescription"]        = "Muestra el historial de actualizaciones de los componentes OpenBase.",
    ["CmdHelpDescription"]           = "Muestra la ayuda para los comandos de OpenBase.",
    ["CmdVersionDescription"]        = "Muestra y gestiona versiones de los componentes OpenBase.",
    ["CmdVersionShowDescription"]    = "Muestra las versiones de la CLI y el template de OpenBase.",
    ["CmdVersionRestoreDescription"] = "Restaura un componente a una versión específica.",
    ["ExtensionNoCsprojFound"]       = "[red]Error:[/] No se encontró ningún archivo .csproj en el directorio actual o sus padres.",
    ["ExtensionAlreadyInstalled"]    = "[yellow]Aviso:[/] La extensión [blue]{0}[/] ya está instalada en este proyecto.",
    ["ExtensionNotFound"]            = "[red]Error:[/] Extensión [yellow]{0}[/] no encontrada. Ejecute [blue]openbase extension list[/] para ver las extensiones disponibles.",
    ["ExtensionInvalidProvider"]     = "[red]Error:[/] El provider [yellow]{0}[/] no es válido para la extensión [yellow]{1}[/]. Disponibles: [blue]{2}[/].",
    ["ExtensionApplyFailed"]         = "[red]Error:[/] Error al aplicar la extensión [yellow]{0}[/]: {1}",
    ["ExtensionAddSuccess"]          = "[green]✓[/] Extensión [blue]{0}[/] agregada con éxito.",
    ["ExtensionRequiresOpenBaseProject"] = "Esta extensión requiere un proyecto OpenBase con Arquitectura Limpia. Ejecute desde la raíz de la solution.",
    ["ExtensionAddingPackage"]       = "  Agregando [blue]{0}[/] a {1}...",
    ["ExtensionPackageAddWarning"]   = "  [yellow]Aviso:[/] No se pudo agregar [yellow]{0}[/]: {1}",
    ["ExtensionAddingReference"]     = "  Agregando referencia [blue]{0}[/] a {1}...",
    ["ExtensionReferenceAddWarning"] = "  [yellow]Aviso:[/] No se pudo agregar la referencia [yellow]{0}[/]: {1}",
    ["ExtensionFileSkipped"]         = "  [yellow]ignorado[/] {0} (ya existe)",
    ["CmdExtensionDescription"]      = "Gestiona extensiones instalables para un proyecto OpenBase.",
    ["CmdExtensionAddDescription"]   = "Agrega una extensión al proyecto actual.",
    ["CmdExtensionListDescription"]  = "Lista todas las extensiones disponibles y su estado de instalación.",
    ["ExtensionListColName"]         = "[bold]Extensión[/]",
    ["ExtensionListColCommand"]      = "[bold]Comando[/]",
    ["ExtensionListColStatus"]       = "[bold]Estado[/]",
    ["ExtensionListStatusInstalled"] = "[green]instalada[/]",
    ["ExtensionListStatusAvailable"] = "[grey]disponible[/]",
    ["JwtProgramCsInjected"]         = "  [green]+[/] Program.cs actualizado con configuración JWT",
    ["JwtProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs ya tiene configuración JWT",
    ["JwtProgramCsNotFound"]         = "  [yellow]Aviso:[/] Program.cs no encontrado — agregue manualmente: builder.Services.AddJwtAuthentication(builder.Configuration); app.UseAuthentication(); app.UseAuthorization();",
    ["JwtProgramCsWarning"]          = "  [yellow]Aviso:[/] No se pudo modificar Program.cs: {0}",
    ["JwtAppSettingsInjected"]       = "  [green]+[/] Sección Jwt agregada a appsettings.json",
    ["JwtAppSettingsWarning"]        = "  [yellow]Aviso:[/] No se pudo modificar appsettings.json: {0}",
    ["JwtControllerProtected"]       = "  [green]+[/] {0} protegida con [[Authorize]]",
    ["JwtSwaggerInjected"]           = "  [green]+[/] Swagger configurado con soporte JWT Bearer",
    ["JwtSwaggerWarning"]            = "  [yellow]Aviso:[/] No se pudo configurar Swagger para JWT: {0}",
    ["CmdRunDescription"]  = "Compila y ejecuta el proyecto, abriendo el browser en Swagger.",
    ["RunNoProjectFound"]  = "[red]Error:[/] Proyecto Presentation.Api de OpenBase no encontrado. Ejecute desde la raíz de la solution.",
    ["RunStarting"]        = "  Iniciando [blue]{0}[/]...",
    ["RunSwaggerUrl"]      = "  Swagger: [blue]{0}[/]",
    ["RunOpeningBrowser"]  = "  Abriendo el browser...",
    ["RunStopped"]         = "\n[grey]Aplicación detenida.[/]",
    ["HelpRunDesc"]        = "Compila y ejecuta el proyecto, abriendo Swagger en el browser",
    ["CmdBuildDescription"]  = "Restaura, compila y prueba el proyecto.",
    ["BuildNoProjectFound"]  = "[red]Error:[/] No se encontró ningún .sln o .csproj en el directorio actual o sus padres.",
    ["BuildRestoring"]       = "  Restaurando...",
    ["BuildBuilding"]        = "  Compilando...",
    ["BuildTesting"]         = "  Ejecutando pruebas...",
    ["BuildStepSuccess"]     = "  [green]✓[/] completado",
    ["BuildStepFailed"]      = "  [red]✗[/] falló",
    ["BuildSuccess"]         = "\n[green]Build completado con éxito.[/]",
    ["BuildFailed"]          = "\n[red]Build fallido.[/]",
    ["HelpBuildDesc"]        = "Restaura, compila y prueba el proyecto",
    ["HealthChecksProgramCsInjected"]          = "  [green]+[/] Program.cs actualizado con configuración de Health Checks",
    ["HealthChecksProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs ya tiene configuración de Health Checks",
    ["HealthChecksProgramCsNotFound"]          = "  [yellow]Aviso:[/] Program.cs no encontrado — agregue manualmente: builder.Services.AddOpenBaseHealthChecks(builder.Configuration); app.MapOpenBaseHealthChecks();",
    ["HealthChecksProgramCsWarning"]           = "  [yellow]Aviso:[/] No se pudo modificar Program.cs: {0}",
    ["RedisProgramCsInjected"]          = "  [green]+[/] Program.cs actualizado con configuración de Redis Cache",
    ["RedisProgramCsAlreadyConfigured"] = "  [yellow]ignorado[/] Program.cs ya tiene configuración de Redis Cache",
    ["RedisProgramCsNotFound"]          = "  [yellow]Aviso:[/] Program.cs no encontrado — agregue manualmente: builder.Services.AddRedisCache(builder.Configuration);",
    ["RedisProgramCsWarning"]           = "  [yellow]Aviso:[/] No se pudo modificar Program.cs: {0}",
    ["RedisAppSettingsInjected"]        = "  [green]+[/] Sección Redis agregada a {0}",
    ["RedisAppSettingsWarning"]         = "  [yellow]Aviso:[/] No se pudo modificar {0}: {1}",
    ["TestingDbConnection"]    = "Probando conexión con la base de datos...",
    ["DbConnectionSuccess"]    = "[green]✓[/] Conexión con la base de datos establecida con éxito.",
    ["DbConnectionFailed"]     = "[yellow]Aviso:[/] No se pudo conectar a la base de datos. Verifique la connection string en appsettings.json.",
    ["ImportFullModelPrompt"]  = "¿Importar todo el modelo de la base de datos (scaffold de todas las tablas)?",
    ["ListingTables"]          = "Listando tablas...",
    ["NoTablesFound"]          = "[yellow]No se encontraron tablas de usuario en la base de datos.[/]",
    ["TablesFound"]            = "{0} tabla(s) encontrada(s). Ingrese el nombre de la entidad para cada una (PascalCase) o deje en blanco para omitir:",
    ["TableEntityNamePrompt"]  = "  [{0}].[{1}] → Nombre de entidad:",
    ["TableSkipped"]           = "  [grey]omitida[/]",
    ["BulkScaffoldSuccess"]    = "\n[green]{0} entidad(es) generada(s) con éxito![/]",
    ["SpecialistAddPrompt"]         = "¿Desea agregar métodos especialistas?",
    ["SpecialistMethodNamePrompt"]  = "Nombre del método (PascalCase):",
    ["SpecialistTypePrompt"]        = "Tipo de método:",
    ["SpecialistQueryChoice"]       = "Query — consulta MediatR (lectura)",
    ["SpecialistCommandChoice"]     = "Command — comando MediatR (escritura)",
    ["SpecialistHttpCallChoice"]    = "HTTP call — llamada externa (no implementada)",
    ["SpecialistAddAnother"]        = "¿Agregar otro método especialista?",
    ["SpecialistMethodRequired"]    = "El nombre del método es obligatorio.",
    ["SpecialistMethodPascalCase"]  = "El nombre del método debe comenzar con mayúscula (PascalCase).",
    ["SpecialistMethodAlphanumeric"] = "El nombre del método debe contener solo letras y números.",
    ["SpecialistMethodReserved"]    = "'{0}' está reservado por el scaffold básico.",
    ["SpecialistFilesCreated"]      = "{0} archivo(s) especialista(s) creado(s):",
    ["SpecialistSqlPrompt"]              = "Ingrese el SQL/operación con parámetros en formato {{ nombreParam }}:",
    ["SpecialistParamsDetected"]         = "Parámetros detectados: {0}",
    ["SpecialistParamTypePrompt"]        = "  {0} — tipo C#:",
    ["SpecialistPaginatedPrompt"]        = "¿La query retorna resultado paginado?",
    ["SpecialistResultColumnsTitle"]     = "Defina las columnas del resultado:",
    ["SpecialistResultColumnName"]       = "  Nombre de columna {0} (Enter para finalizar):",
    ["SpecialistResultColumnTypePrompt"] = "  {0} — tipo C#:",
    ["ScaffoldUpdateEntityNotFound"]  = "[red]Error:[/] Entidad [blue]{0}[/] no encontrada. Ejecute [blue]openbase scaffold --entity {0}[/] primero.",
    ["ScaffoldUpdateNoChanges"]       = "[green]✓[/] No se detectaron diferencias. Los archivos ya están actualizados.",
    ["ScaffoldUpdateDiffTitle"]       = "\nDiferencias detectadas:",
    ["ScaffoldUpdateUncommittedWarn"] = "[yellow]⚠️  Los siguientes archivos tienen cambios no confirmados:[/]",
    ["ScaffoldUpdateUncommittedHint"] = "[grey]Considere hacer commit o stash antes de continuar.[/]",
    ["ScaffoldUpdateApplyChanges"]    = "¿Aplicar cambios?",
    ["ScaffoldUpdateDestructiveWarn"] = "[yellow]⚠️  {0} propiedad(es) serán ELIMINADAS de múltiples archivos.[/]",
    ["ScaffoldUpdateConfirmRemoval"]  = "¿Confirmar eliminación?",
    ["FilesUpdated"]                  = "{0} archivo(s) actualizado(s):",
    ["ScaffoldUpdateSuccess"]         = "\n[green]Scaffold de la entidad [bold]{0}[/] actualizado con éxito![/]",
    ["ScaffoldUpdateMigrationHint"]   = "[grey]No olvide crear la migración: [blue]dotnet ef migrations add Update{0}[/][/]",
}) { }


public static class SR
{
    public static IStrings Current { get; private set; } = new EnStrings();

    public static void Configure()
    {
        var culture = CultureInfo.CurrentUICulture.Name;

        if (culture.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            Current = new PtBrStrings();
        else if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            Current = new EsStrings();
        else
            Current = new EnStrings();
    }
}
