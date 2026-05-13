using System.Globalization;

namespace OpenBase.CLI.Localization;

public interface IStrings
{
    // ── Scaffold ──────────────────────────────────────────────────────────────
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
    string HowToGenerateScaffold { get; }
    string CodeFirstChoice { get; }
    string ModelFirstChoice { get; }
    string ModelFirstReconciliationInfo { get; }   // {0}=entity
    string ModelFirstReconciliationSuccess { get; }
    string ModelFirstReconciliationWarn { get; }   // {0}=entity

    // ── New ───────────────────────────────────────────────────────────────────
    string NameParamRequired { get; }
    string ProjectNameInvalid { get; }
    string SdkIncompatible { get; }
    string SdkUpdateRequired { get; }         // {0}=version
    string ApiDatabasePrompt { get; }
    string InvalidTypeCombination { get; }    // {0}=type {1}=template
    string AvailableCombinations { get; }
    string CreatingProject { get; }           // {0}=name
    string CreateProjectFailed { get; }

    // ── Project Configurator ──────────────────────────────────────────────────
    string ProjectConfiguration { get; }
    string MediatRLicense { get; }
    string AutoMapperLicense { get; }
    string DatabaseServer { get; }
    string DatabaseName { get; }
    string DatabaseUser { get; }
    string DatabasePassword { get; }

    // ── Entity Property Collector ─────────────────────────────────────────────
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

    // ── Model First Collector ─────────────────────────────────────────────────
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

    // ── Update / Install ──────────────────────────────────────────────────────
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

    // ── Version Restore ───────────────────────────────────────────────────────
    string UseTypeToSpecify { get; }
    string InvalidType { get; }               // {0}=type
    string RestoringToVersion { get; }        // {0}=displayName {1}=version
    string ApplyingVersion { get; }           // {0}=version
    string RestoreFailed { get; }             // {0}=displayName {1}=version
    string RestoreSuccess { get; }            // {0}=displayName {1}=version

    // ── History ───────────────────────────────────────────────────────────────
    string InvalidTypeHistory { get; }        // {0}=type
    string NoHistoryFound { get; }
    string ColDate { get; }
    string ColComponent { get; }
    string ColPreviousVersion { get; }
    string ColNewVersion { get; }
    string ColStatus { get; }

    // ── Help ──────────────────────────────────────────────────────────────────
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
    string HelpTip { get; }
    string HelpSupport { get; }

    // ── Version ───────────────────────────────────────────────────────────────
    string ColVersionComponent { get; }
    string ColVersion { get; }

    // ── Program descriptions ──────────────────────────────────────────────────
    string CmdInstallDescription { get; }
    string CmdUpdateDescription { get; }
    string CmdNewDescription { get; }
    string CmdScaffoldDescription { get; }
    string CmdHistoryDescription { get; }
    string CmdHelpDescription { get; }
    string CmdVersionDescription { get; }
    string CmdVersionShowDescription { get; }
    string CmdVersionRestoreDescription { get; }
}

// ─────────────────────────────────────────────────────────────────────────────

public sealed class EnStrings : IStrings
{
    public string EntityParamRequired => "The --entity <ENTITY> parameter is required.";
    public string EntityMustBePascalCase => "The entity name must start with an uppercase letter (PascalCase).";
    public string EntityMustBeAlphanumeric => "The entity name must contain only letters and numbers.";
    public string ProjectStructureNotFound => "[red]Error:[/] OpenBase project structure not found.";
    public string RunInProjectRoot => "Run this command at the root of a project created with [blue]openbase new[/].";
    public string OrProvideNamespace => "Or provide the namespace with [blue]--namespace <NAMESPACE>[/].";
    public string ProceedWithScaffold => "Proceed with scaffold?";
    public string GeneratingScaffold => "Generating scaffold for [blue]{0}[/]...";
    public string FilesCreated => "{0} file(s) created:";
    public string FilesSkipped => "{0} file(s) already exist, skipped:";
    public string FilesErrors => "{0} error(s):";
    public string ScaffoldSuccess => "\n[green]Scaffold for entity [bold]{0}[/] generated successfully![/]";
    public string NextSteps => "Next steps:";
    public string AddDbSet => "  1. Add [blue]DbSet<{0}> {1} {{ get; set; }}[/] to DbContext";
    public string RunMigrationsAdd => "  2. Run [blue]dotnet ef migrations add Add{0}[/]";
    public string RunDatabaseUpdate => "  3. Run [blue]dotnet ef database update[/]";
    public string RestoringNuGetPackages => "Restoring NuGet packages...";
    public string RestorePackagesWarning => "[yellow]Warning:[/] Failed to restore packages. Trying to generate the migration anyway...";
    public string GeneratingMigration => "Generating migration [blue]Add{0}[/]...";
    public string MigrationFailed => "[red]Error:[/] Failed to generate the migration.";
    public string RunMigrationManually => "Run manually: [blue]dotnet ef migrations add Add{0}[/]";
    public string MigrationGenerated => "[green]Migration Add{0} generated.[/]";
    public string RunDatabaseUpdateNow => "Run [blue]database update[/] now?";
    public string ExecutingDatabaseUpdate => "Running [blue]database update[/]...";
    public string DatabaseUpdateFailed => "[red]Error:[/] Failed to run database update.";
    public string DotnetEfDatabaseUpdate => "[blue]dotnet ef database update[/]";
    public string DatabaseUpdatedSuccess => "[green]Database updated successfully.[/]";
    public string HowToGenerateScaffold => "\nHow would you like to generate the scaffold?";
    public string CodeFirstChoice => "Code First (define properties manually)";
    public string ModelFirstChoice => "Model First (read structure from an existing table)";
    public string ModelFirstReconciliationInfo => "Registering [blue]Add{0}[/] in EF migration history (table already exists)...";
    public string ModelFirstReconciliationSuccess => "[green]✓[/] Existing table registered. Future Code First migrations will not try to recreate it.";
    public string ModelFirstReconciliationWarn => "[yellow]Warning:[/] Could not register the existing table. Before running [blue]database update[/] in the future, manually empty the Up() body in the migration for Add{0}.";

    public string NameParamRequired => "The --name <NAME> parameter is required.";
    public string ProjectNameInvalid => "The project name contains invalid characters. Use only letters, numbers, '-' and '_'.";
    public string SdkIncompatible => "[red]Error:[/] The installed .NET SDK is incompatible with this version of OpenBase.";
    public string SdkUpdateRequired => ".NET [blue]{0}[/] or higher is required. Update the SDK at: [blue]https://dot.net[/]";
    public string ApiDatabasePrompt => "API database:";
    public string InvalidTypeCombination => "[red]Error:[/] The combination Type '[yellow]{0}[/]' + Template '[yellow]{1}[/]' is not valid.";
    public string AvailableCombinations => "Available combinations: [blue]--type api --template sqlserver[/]";
    public string CreatingProject => "Creating project [blue]{0}[/]...";
    public string CreateProjectFailed => "[red]Error:[/] Failed to create the project. Make sure the template is installed with [blue]openbase install[/].";

    public string ProjectConfiguration => "[bold]Project configuration[/]";
    public string MediatRLicense => "[blue]MediatR[/] license [grey](leave blank if you don't have one)[/]:";
    public string AutoMapperLicense => "[blue]AutoMapper[/] license [grey](leave blank if you don't have one)[/]:";
    public string DatabaseServer => "Database server:";
    public string DatabaseName => "Database name:";
    public string DatabaseUser => "Database user:";
    public string DatabasePassword => "Database password:";

    public string EntityProperties => "[bold]Entity properties[/]";
    public string DatabaseAndTypes => "[grey]Database: [blue]{0}[/] | Available types: {1}[/]";
    public string PropertyNamePrompt => "[bold]Prop {0}[/] — Name [grey](PascalCase)[/]:";
    public string PropertyTypePrompt => "  Type:";
    public string PropertyNotNull => "  Not null (required)?";
    public string PropertyAdded => "  [green]+ {0} ({1})[/]";
    public string AddAnotherProperty => "Add another property?";
    public string ColProperty => "Property";
    public string ColType => "Type";
    public string ColPK => "PK";
    public string ColNotNull => "Not Null";
    public string PropNameRequired => "Name is required.";
    public string PropNameMustStartUpper => "Must start with an uppercase letter.";
    public string PropNameAlphanumericOnly => "Use only letters and numbers.";
    public string PropNameIdReserved => "'Id' is reserved as the primary key.";
    public string PropNameAlreadyAdded => "Property '{0}' has already been added.";

    public string SchemaOwnerPrompt => "Schema/owner [[{0}]]:";
    public string TableNamePrompt => "Table name:";
    public string TableNameRequired => "Please enter the table name.";
    public string ConnectionStringNotFound => "[yellow]Connection string not found in appsettings.json.[/]\nEnter the connection string:";
    public string ConnectionStringRequired => "The connection string is required.";
    public string ReadingTableStructure => "Reading table structure [blue]{0}.{1}[/]...";
    public string ErrorReadingTable => "[red]Error reading table:[/] {0}";
    public string NoColumnsFound => "[red]No columns found in table [bold]{0}.{1}[/].[/]";
    public string CheckSchemaAndTableName => "Check that the schema and table names are correct.";
    public string ColCsType => "C# Type";

    public string SyncingTemplates => "[blue]Syncing OpenBase templates...[/]";
    public string PackageStatusVerb => "Updating";
    public string PackageSuccessLabel => "updated";
    public string PackageErrorLabel => "update";
    public string PackageOperationFailed => "[red]Error:[/] Failed to {0} [yellow]{1}[/].";
    public string PackageOperationSuccess => "[green]✓[/] {0} {1}.";
    public string InstallStarting => "[blue]Starting OpenBase package installation...[/]";
    public string InstallStatusVerb => "Installing";
    public string InstallSuccessLabel => "installed";
    public string InstallErrorLabel => "install";
    public string UpdatingCli => "Updating OpenBase CLI...";
    public string UpdateCliFailed => "[red]Error:[/] Failed to update the OpenBase CLI.";
    public string SomeComponentsUpdateFailed => "[yellow]Warning:[/] Some components could not be updated.";
    public string CliUpdated => "[green]✓[/] OpenBase CLI updated.";

    public string UseTypeToSpecify => "Use --type to specify the component: cli, sqlserver, postgres";
    public string InvalidType => "Invalid type '{0}'. Use: cli, sqlserver, postgres";
    public string RestoringToVersion => "[blue]Restoring {0} to version {1}...[/]";
    public string ApplyingVersion => "Applying version {0}...";
    public string RestoreFailed => "[red]Error:[/] Failed to restore {0} to version {1}.";
    public string RestoreSuccess => "[green]✓[/] {0} restored to version {1}.";

    public string InvalidTypeHistory => "[red]Error:[/] Invalid type [yellow]{0}[/]. Use: cli, sqlserver, postgres";
    public string NoHistoryFound => "[grey]No update history found.[/]";
    public string ColDate => "[bold]Date[/]";
    public string ColComponent => "[bold]Component[/]";
    public string ColPreviousVersion => "[bold]Previous Version[/]";
    public string ColNewVersion => "[bold]New Version[/]";
    public string ColStatus => "[bold]Status[/]";

    public string HelpSubtitle => "[grey]Productivity CLI for Clean Architecture[/]";
    public string HelpColCommand => "[bold yellow]Command[/]";
    public string HelpColDescription => "[bold yellow]Description[/]";
    public string HelpColExample => "[bold yellow]Usage example[/]";
    public string HelpInstallDesc => "Installs all OpenBase NuGet templates";
    public string HelpNewDesc => "Creates a new structured project";
    public string HelpScaffoldDesc => "Generates all CRUD layers for an entity (Domain, Application, Infra, Presentation)";
    public string HelpHistoryDesc => "Shows update history (--type cli | sqlserver | postgres)";
    public string HelpUpdateDesc => "Syncs and updates templates and CLI";
    public string HelpVersionShowDesc => "Shows versions of the installed environment";
    public string HelpVersionRestoreDesc => "Restores a component to a specific version (--type cli | sqlserver | postgres)";
    public string HelpTip => "[bold white]Tip:[/] Use [blue]--help[/] after any command to see technical details.";
    public string HelpSupport => " Support ";

    public string ColVersionComponent => "[bold]Component[/]";
    public string ColVersion => "[bold]Version[/]";

    public string CmdInstallDescription => "Installs the OpenBase template ecosystem.";
    public string CmdUpdateDescription => "Syncs and updates all OpenBase templates.";
    public string CmdNewDescription => "Creates a new project based on a template.";
    public string CmdScaffoldDescription => "Generates all architecture layers for an entity.";
    public string CmdHistoryDescription => "Shows the update history of OpenBase components.";
    public string CmdHelpDescription => "Displays help for OpenBase commands.";
    public string CmdVersionDescription => "Displays and manages versions of OpenBase components.";
    public string CmdVersionShowDescription => "Displays the versions of the OpenBase CLI and template.";
    public string CmdVersionRestoreDescription => "Restores a component to a specific version.";
}

// ─────────────────────────────────────────────────────────────────────────────

public sealed class PtBrStrings : IStrings
{
    public string EntityParamRequired => "O parâmetro --entity <ENTIDADE> é obrigatório.";
    public string EntityMustBePascalCase => "O nome da entidade deve começar com letra maiúscula (PascalCase).";
    public string EntityMustBeAlphanumeric => "O nome da entidade deve conter apenas letras e números.";
    public string ProjectStructureNotFound => "[red]Erro:[/] Estrutura de projeto OpenBase não encontrada.";
    public string RunInProjectRoot => "Execute este comando na raiz de um projeto criado com [blue]openbase new[/].";
    public string OrProvideNamespace => "Ou informe o namespace com [blue]--namespace <NAMESPACE>[/].";
    public string ProceedWithScaffold => "Prosseguir com o scaffold?";
    public string GeneratingScaffold => "Gerando scaffold para [blue]{0}[/]...";
    public string FilesCreated => "{0} arquivo(s) criado(s):";
    public string FilesSkipped => "{0} arquivo(s) já existente(s) ignorado(s):";
    public string FilesErrors => "{0} erro(s):";
    public string ScaffoldSuccess => "\n[green]Scaffold da entidade [bold]{0}[/] gerado com sucesso![/]";
    public string NextSteps => "Próximos passos:";
    public string AddDbSet => "  1. Adicione [blue]DbSet<{0}> {1} {{ get; set; }}[/] ao DbContext";
    public string RunMigrationsAdd => "  2. Execute [blue]dotnet ef migrations add Add{0}[/]";
    public string RunDatabaseUpdate => "  3. Execute [blue]dotnet ef database update[/]";
    public string RestoringNuGetPackages => "Restaurando pacotes NuGet...";
    public string RestorePackagesWarning => "[yellow]Aviso:[/] Falha ao restaurar pacotes. Tentando gerar a migration mesmo assim...";
    public string GeneratingMigration => "Gerando migration [blue]Add{0}[/]...";
    public string MigrationFailed => "[red]Erro:[/] Falha ao gerar a migration.";
    public string RunMigrationManually => "Execute manualmente: [blue]dotnet ef migrations add Add{0}[/]";
    public string MigrationGenerated => "[green]Migration Add{0} gerada.[/]";
    public string RunDatabaseUpdateNow => "Executar [blue]database update[/] agora?";
    public string ExecutingDatabaseUpdate => "Executando [blue]database update[/]...";
    public string DatabaseUpdateFailed => "[red]Erro:[/] Falha ao executar database update.";
    public string DotnetEfDatabaseUpdate => "[blue]dotnet ef database update[/]";
    public string DatabaseUpdatedSuccess => "[green]Banco de dados atualizado com sucesso.[/]";
    public string HowToGenerateScaffold => "\nComo deseja gerar o scaffold?";
    public string CodeFirstChoice => "Code First (definir propriedades manualmente)";
    public string ModelFirstChoice => "Model First (ler estrutura de uma tabela existente)";
    public string ModelFirstReconciliationInfo => "Registrando [blue]Add{0}[/] no histórico de migrations do EF (tabela já existe)...";
    public string ModelFirstReconciliationSuccess => "[green]✓[/] Tabela existente registrada. Migrations futuras do Code First não tentarão recriá-la.";
    public string ModelFirstReconciliationWarn => "[yellow]Aviso:[/] Não foi possível registrar a tabela existente. Antes de executar [blue]database update[/] no futuro, esvazie manualmente o corpo do Up() na migration Add{0}.";

    public string NameParamRequired => "O parâmetro --name <NOME> é obrigatório.";
    public string ProjectNameInvalid => "O nome do projeto contém caracteres inválidos. Use apenas letras, números, '-' e '_'.";
    public string SdkIncompatible => "[red]Erro:[/] O .NET SDK instalado é incompatível com esta versão do OpenBase.";
    public string SdkUpdateRequired => "É necessário o [blue].NET {0}[/] ou superior. Atualize o SDK em: [blue]https://dot.net[/]";
    public string ApiDatabasePrompt => "Banco de dados da API:";
    public string InvalidTypeCombination => "[red]Erro:[/] A combinação Tipo '[yellow]{0}[/]' + Template '[yellow]{1}[/]' não é válida.";
    public string AvailableCombinations => "Combinações disponíveis: [blue]--type api --template sqlserver[/]";
    public string CreatingProject => "Criando projeto [blue]{0}[/]...";
    public string CreateProjectFailed => "[red]Erro:[/] Falha ao criar o projeto. Verifique se o template está instalado com [blue]openbase install[/].";

    public string ProjectConfiguration => "[bold]Configuração do projeto[/]";
    public string MediatRLicense => "Licença do [blue]MediatR[/] [grey](deixe em branco se não tiver)[/]:";
    public string AutoMapperLicense => "Licença do [blue]AutoMapper[/] [grey](deixe em branco se não tiver)[/]:";
    public string DatabaseServer => "Servidor do banco de dados:";
    public string DatabaseName => "Nome do banco de dados:";
    public string DatabaseUser => "Usuário do banco de dados:";
    public string DatabasePassword => "Senha do banco de dados:";

    public string EntityProperties => "[bold]Propriedades da entidade[/]";
    public string DatabaseAndTypes => "[grey]Banco: [blue]{0}[/] | Tipos disponíveis: {1}[/]";
    public string PropertyNamePrompt => "[bold]Prop {0}[/] — Nome [grey](PascalCase)[/]:";
    public string PropertyTypePrompt => "  Tipo:";
    public string PropertyNotNull => "  Not null (obrigatório)?";
    public string PropertyAdded => "  [green]+ {0} ({1})[/]";
    public string AddAnotherProperty => "Adicionar outra propriedade?";
    public string ColProperty => "Propriedade";
    public string ColType => "Tipo";
    public string ColPK => "PK";
    public string ColNotNull => "Not Null";
    public string PropNameRequired => "O nome é obrigatório.";
    public string PropNameMustStartUpper => "Deve começar com letra maiúscula.";
    public string PropNameAlphanumericOnly => "Use apenas letras e números.";
    public string PropNameIdReserved => "'Id' é reservado como chave primária.";
    public string PropNameAlreadyAdded => "Propriedade '{0}' já foi adicionada.";

    public string SchemaOwnerPrompt => "Schema/owner [[{0}]]:";
    public string TableNamePrompt => "Nome da tabela:";
    public string TableNameRequired => "Informe o nome da tabela.";
    public string ConnectionStringNotFound => "[yellow]Connection string não encontrada no appsettings.json.[/]\nInforme a connection string:";
    public string ConnectionStringRequired => "A connection string é obrigatória.";
    public string ReadingTableStructure => "Lendo estrutura da tabela [blue]{0}.{1}[/]...";
    public string ErrorReadingTable => "[red]Erro ao ler a tabela:[/] {0}";
    public string NoColumnsFound => "[red]Nenhuma coluna encontrada na tabela [bold]{0}.{1}[/].[/]";
    public string CheckSchemaAndTableName => "Verifique se o nome do schema e da tabela estão corretos.";
    public string ColCsType => "Tipo C#";

    public string SyncingTemplates => "[blue]Sincronizando templates OpenBase...[/]";
    public string PackageStatusVerb => "Atualizando";
    public string PackageSuccessLabel => "atualizado";
    public string PackageErrorLabel => "atualizar";
    public string PackageOperationFailed => "[red]Erro:[/] Falha ao {0} [yellow]{1}[/].";
    public string PackageOperationSuccess => "[green]✓[/] {0} {1}.";
    public string InstallStarting => "[blue]Iniciando a instalação dos pacotes OpenBase...[/]";
    public string InstallStatusVerb => "Instalando";
    public string InstallSuccessLabel => "instalado";
    public string InstallErrorLabel => "instalar";
    public string UpdatingCli => "Atualizando OpenBase CLI...";
    public string UpdateCliFailed => "[red]Erro:[/] Falha ao atualizar a OpenBase CLI.";
    public string SomeComponentsUpdateFailed => "[yellow]Aviso:[/] Alguns componentes não puderam ser atualizados.";
    public string CliUpdated => "[green]✓[/] OpenBase CLI atualizada.";

    public string UseTypeToSpecify => "Use --type para especificar o componente: cli, sqlserver, postgres";
    public string InvalidType => "Tipo inválido '{0}'. Use: cli, sqlserver, postgres";
    public string RestoringToVersion => "[blue]Restaurando {0} para a versão {1}...[/]";
    public string ApplyingVersion => "Aplicando versão {0}...";
    public string RestoreFailed => "[red]Erro:[/] Falha ao restaurar {0} para a versão {1}.";
    public string RestoreSuccess => "[green]✓[/] {0} restaurado para a versão {1}.";

    public string InvalidTypeHistory => "[red]Erro:[/] Tipo inválido [yellow]{0}[/]. Use: cli, sqlserver, postgres";
    public string NoHistoryFound => "[grey]Nenhum histórico de atualização encontrado.[/]";
    public string ColDate => "[bold]Data[/]";
    public string ColComponent => "[bold]Componente[/]";
    public string ColPreviousVersion => "[bold]Versão Anterior[/]";
    public string ColNewVersion => "[bold]Nova Versão[/]";
    public string ColStatus => "[bold]Status[/]";

    public string HelpSubtitle => "[grey]CLI de produtividade para Arquitetura Limpa[/]";
    public string HelpColCommand => "[bold yellow]Comando[/]";
    public string HelpColDescription => "[bold yellow]Descrição[/]";
    public string HelpColExample => "[bold yellow]Exemplo de uso[/]";
    public string HelpInstallDesc => "Instala todos os templates NuGet do OpenBase";
    public string HelpNewDesc => "Cria um novo projeto estruturado";
    public string HelpScaffoldDesc => "Gera todas as camadas CRUD de uma entidade (Domain, Application, Infra, Presentation)";
    public string HelpHistoryDesc => "Exibe o histórico de atualizações (--type cli | sqlserver | postgres)";
    public string HelpUpdateDesc => "Sincroniza e atualiza templates e a CLI";
    public string HelpVersionShowDesc => "Mostra versões do ambiente instalado";
    public string HelpVersionRestoreDesc => "Restaura um componente para uma versão específica (--type cli | sqlserver | postgres)";
    public string HelpTip => "[bold white]Dica:[/] Use [blue]--help[/] após qualquer comando para ver detalhes técnicos.";
    public string HelpSupport => " Suporte ";

    public string ColVersionComponent => "[bold]Componente[/]";
    public string ColVersion => "[bold]Versão[/]";

    public string CmdInstallDescription => "Instala o ecossistema de templates OpenBase.";
    public string CmdUpdateDescription => "Sincroniza e atualiza todos os templates OpenBase.";
    public string CmdNewDescription => "Cria um novo projeto baseado em um template.";
    public string CmdScaffoldDescription => "Gera todas as camadas da arquitetura para uma entidade.";
    public string CmdHistoryDescription => "Exibe o histórico de atualizações dos componentes OpenBase.";
    public string CmdHelpDescription => "Exibe a ajuda para os comandos do OpenBase.";
    public string CmdVersionDescription => "Exibe e gerencia versões dos componentes OpenBase.";
    public string CmdVersionShowDescription => "Exibe as versões da CLI e do template do OpenBase.";
    public string CmdVersionRestoreDescription => "Restaura um componente para uma versão específica.";
}

// ─────────────────────────────────────────────────────────────────────────────

public sealed class EsStrings : IStrings
{
    public string EntityParamRequired => "El parámetro --entity <ENTIDAD> es obligatorio.";
    public string EntityMustBePascalCase => "El nombre de la entidad debe comenzar con mayúscula (PascalCase).";
    public string EntityMustBeAlphanumeric => "El nombre de la entidad debe contener solo letras y números.";
    public string ProjectStructureNotFound => "[red]Error:[/] No se encontró la estructura de proyecto OpenBase.";
    public string RunInProjectRoot => "Ejecute este comando en la raíz de un proyecto creado con [blue]openbase new[/].";
    public string OrProvideNamespace => "O indique el namespace con [blue]--namespace <NAMESPACE>[/].";
    public string ProceedWithScaffold => "¿Continuar con el scaffold?";
    public string GeneratingScaffold => "Generando scaffold para [blue]{0}[/]...";
    public string FilesCreated => "{0} archivo(s) creado(s):";
    public string FilesSkipped => "{0} archivo(s) ya existente(s), omitido(s):";
    public string FilesErrors => "{0} error(es):";
    public string ScaffoldSuccess => "\n[green]Scaffold de la entidad [bold]{0}[/] generado con éxito![/]";
    public string NextSteps => "Próximos pasos:";
    public string AddDbSet => "  1. Agregue [blue]DbSet<{0}> {1} {{ get; set; }}[/] al DbContext";
    public string RunMigrationsAdd => "  2. Ejecute [blue]dotnet ef migrations add Add{0}[/]";
    public string RunDatabaseUpdate => "  3. Ejecute [blue]dotnet ef database update[/]";
    public string RestoringNuGetPackages => "Restaurando paquetes NuGet...";
    public string RestorePackagesWarning => "[yellow]Aviso:[/] Error al restaurar paquetes. Intentando generar la migración de todos modos...";
    public string GeneratingMigration => "Generando migración [blue]Add{0}[/]...";
    public string MigrationFailed => "[red]Error:[/] Error al generar la migración.";
    public string RunMigrationManually => "Ejecute manualmente: [blue]dotnet ef migrations add Add{0}[/]";
    public string MigrationGenerated => "[green]Migración Add{0} generada.[/]";
    public string RunDatabaseUpdateNow => "¿Ejecutar [blue]database update[/] ahora?";
    public string ExecutingDatabaseUpdate => "Ejecutando [blue]database update[/]...";
    public string DatabaseUpdateFailed => "[red]Error:[/] Error al ejecutar database update.";
    public string DotnetEfDatabaseUpdate => "[blue]dotnet ef database update[/]";
    public string DatabaseUpdatedSuccess => "[green]Base de datos actualizada con éxito.[/]";
    public string HowToGenerateScaffold => "\n¿Cómo desea generar el scaffold?";
    public string CodeFirstChoice => "Code First (definir propiedades manualmente)";
    public string ModelFirstChoice => "Model First (leer estructura de una tabla existente)";
    public string ModelFirstReconciliationInfo => "Registrando [blue]Add{0}[/] en el historial de migraciones de EF (la tabla ya existe)...";
    public string ModelFirstReconciliationSuccess => "[green]✓[/] Tabla existente registrada. Las migraciones futuras de Code First no intentarán recrearla.";
    public string ModelFirstReconciliationWarn => "[yellow]Aviso:[/] No se pudo registrar la tabla existente. Antes de ejecutar [blue]database update[/] en el futuro, vacíe manualmente el cuerpo de Up() en la migración Add{0}.";

    public string NameParamRequired => "El parámetro --name <NOMBRE> es obligatorio.";
    public string ProjectNameInvalid => "El nombre del proyecto contiene caracteres no válidos. Use solo letras, números, '-' y '_'.";
    public string SdkIncompatible => "[red]Error:[/] El .NET SDK instalado es incompatible con esta versión de OpenBase.";
    public string SdkUpdateRequired => "Se requiere [blue].NET {0}[/] o superior. Actualice el SDK en: [blue]https://dot.net[/]";
    public string ApiDatabasePrompt => "Base de datos de la API:";
    public string InvalidTypeCombination => "[red]Error:[/] La combinación Tipo '[yellow]{0}[/]' + Template '[yellow]{1}[/]' no es válida.";
    public string AvailableCombinations => "Combinaciones disponibles: [blue]--type api --template sqlserver[/]";
    public string CreatingProject => "Creando proyecto [blue]{0}[/]...";
    public string CreateProjectFailed => "[red]Error:[/] Error al crear el proyecto. Verifique que el template esté instalado con [blue]openbase install[/].";

    public string ProjectConfiguration => "[bold]Configuración del proyecto[/]";
    public string MediatRLicense => "Licencia de [blue]MediatR[/] [grey](deje en blanco si no tiene)[/]:";
    public string AutoMapperLicense => "Licencia de [blue]AutoMapper[/] [grey](deje en blanco si no tiene)[/]:";
    public string DatabaseServer => "Servidor de base de datos:";
    public string DatabaseName => "Nombre de la base de datos:";
    public string DatabaseUser => "Usuario de la base de datos:";
    public string DatabasePassword => "Contraseña de la base de datos:";

    public string EntityProperties => "[bold]Propiedades de la entidad[/]";
    public string DatabaseAndTypes => "[grey]Base de datos: [blue]{0}[/] | Tipos disponibles: {1}[/]";
    public string PropertyNamePrompt => "[bold]Prop {0}[/] — Nombre [grey](PascalCase)[/]:";
    public string PropertyTypePrompt => "  Tipo:";
    public string PropertyNotNull => "  ¿Not null (obligatorio)?";
    public string PropertyAdded => "  [green]+ {0} ({1})[/]";
    public string AddAnotherProperty => "¿Agregar otra propiedad?";
    public string ColProperty => "Propiedad";
    public string ColType => "Tipo";
    public string ColPK => "PK";
    public string ColNotNull => "Not Null";
    public string PropNameRequired => "El nombre es obligatorio.";
    public string PropNameMustStartUpper => "Debe comenzar con letra mayúscula.";
    public string PropNameAlphanumericOnly => "Use solo letras y números.";
    public string PropNameIdReserved => "'Id' está reservado como clave primaria.";
    public string PropNameAlreadyAdded => "La propiedad '{0}' ya fue agregada.";

    public string SchemaOwnerPrompt => "Schema/owner [[{0}]]:";
    public string TableNamePrompt => "Nombre de la tabla:";
    public string TableNameRequired => "Ingrese el nombre de la tabla.";
    public string ConnectionStringNotFound => "[yellow]Connection string no encontrada en appsettings.json.[/]\nIngrese la connection string:";
    public string ConnectionStringRequired => "La connection string es obligatoria.";
    public string ReadingTableStructure => "Leyendo estructura de la tabla [blue]{0}.{1}[/]...";
    public string ErrorReadingTable => "[red]Error al leer la tabla:[/] {0}";
    public string NoColumnsFound => "[red]No se encontraron columnas en la tabla [bold]{0}.{1}[/].[/]";
    public string CheckSchemaAndTableName => "Verifique que el nombre del schema y de la tabla sean correctos.";
    public string ColCsType => "Tipo C#";

    public string SyncingTemplates => "[blue]Sincronizando templates OpenBase...[/]";
    public string PackageStatusVerb => "Actualizando";
    public string PackageSuccessLabel => "actualizado";
    public string PackageErrorLabel => "actualizar";
    public string PackageOperationFailed => "[red]Error:[/] Error al {0} [yellow]{1}[/].";
    public string PackageOperationSuccess => "[green]✓[/] {0} {1}.";
    public string InstallStarting => "[blue]Iniciando la instalación de los paquetes OpenBase...[/]";
    public string InstallStatusVerb => "Instalando";
    public string InstallSuccessLabel => "instalado";
    public string InstallErrorLabel => "instalar";
    public string UpdatingCli => "Actualizando OpenBase CLI...";
    public string UpdateCliFailed => "[red]Error:[/] Error al actualizar la OpenBase CLI.";
    public string SomeComponentsUpdateFailed => "[yellow]Aviso:[/] Algunos componentes no pudieron actualizarse.";
    public string CliUpdated => "[green]✓[/] OpenBase CLI actualizada.";

    public string UseTypeToSpecify => "Use --type para especificar el componente: cli, sqlserver, postgres";
    public string InvalidType => "Tipo inválido '{0}'. Use: cli, sqlserver, postgres";
    public string RestoringToVersion => "[blue]Restaurando {0} a la versión {1}...[/]";
    public string ApplyingVersion => "Aplicando versión {0}...";
    public string RestoreFailed => "[red]Error:[/] Error al restaurar {0} a la versión {1}.";
    public string RestoreSuccess => "[green]✓[/] {0} restaurado a la versión {1}.";

    public string InvalidTypeHistory => "[red]Error:[/] Tipo inválido [yellow]{0}[/]. Use: cli, sqlserver, postgres";
    public string NoHistoryFound => "[grey]No se encontró historial de actualizaciones.[/]";
    public string ColDate => "[bold]Fecha[/]";
    public string ColComponent => "[bold]Componente[/]";
    public string ColPreviousVersion => "[bold]Versión Anterior[/]";
    public string ColNewVersion => "[bold]Nueva Versión[/]";
    public string ColStatus => "[bold]Estado[/]";

    public string HelpSubtitle => "[grey]CLI de productividad para Arquitectura Limpia[/]";
    public string HelpColCommand => "[bold yellow]Comando[/]";
    public string HelpColDescription => "[bold yellow]Descripción[/]";
    public string HelpColExample => "[bold yellow]Ejemplo de uso[/]";
    public string HelpInstallDesc => "Instala todos los templates NuGet de OpenBase";
    public string HelpNewDesc => "Crea un nuevo proyecto estructurado";
    public string HelpScaffoldDesc => "Genera todas las capas CRUD de una entidad (Domain, Application, Infra, Presentation)";
    public string HelpHistoryDesc => "Muestra el historial de actualizaciones (--type cli | sqlserver | postgres)";
    public string HelpUpdateDesc => "Sincroniza y actualiza templates y la CLI";
    public string HelpVersionShowDesc => "Muestra versiones del entorno instalado";
    public string HelpVersionRestoreDesc => "Restaura un componente a una versión específica (--type cli | sqlserver | postgres)";
    public string HelpTip => "[bold white]Consejo:[/] Use [blue]--help[/] después de cualquier comando para ver detalles técnicos.";
    public string HelpSupport => " Soporte ";

    public string ColVersionComponent => "[bold]Componente[/]";
    public string ColVersion => "[bold]Versión[/]";

    public string CmdInstallDescription => "Instala el ecosistema de templates OpenBase.";
    public string CmdUpdateDescription => "Sincroniza y actualiza todos los templates OpenBase.";
    public string CmdNewDescription => "Crea un nuevo proyecto basado en un template.";
    public string CmdScaffoldDescription => "Genera todas las capas de arquitectura para una entidad.";
    public string CmdHistoryDescription => "Muestra el historial de actualizaciones de los componentes OpenBase.";
    public string CmdHelpDescription => "Muestra la ayuda para los comandos de OpenBase.";
    public string CmdVersionDescription => "Muestra y gestiona versiones de los componentes OpenBase.";
    public string CmdVersionShowDescription => "Muestra las versiones de la CLI y el template de OpenBase.";
    public string CmdVersionRestoreDescription => "Restaura un componente a una versión específica.";
}

// ─────────────────────────────────────────────────────────────────────────────

public static class SR
{
    public static IStrings Current { get; private set; } = new EnStrings();

    public static void Configure()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        Current = culture.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? new PtBrStrings()
                : culture.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? new EsStrings()
                : new EnStrings();
    }
}
