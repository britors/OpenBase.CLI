using System.ComponentModel;
using System.Data.Common;
using System.Text.Json;
using OpenBase.CLI.Helpers.Creation;
using OpenBase.CLI.Helpers.Execution;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class NewSettings : CommandSettings
{
    [CommandOption("-t|--type <TYPE>")]
    [Description("Tipo de projeto: api")]
    public string Type { get; set; } = "api";
    [CommandOption("-d|--database <DATABASE>")]
    [Description("postgres, sqlserver ou oracle (pgsql/postgresql são aliases)")]
    public string[] Databases { get; set; } = [];
    [CommandOption("-s|--template <TEMPLATE>")]
    [Description("Alias legado de --database; será removido em uma major futura")]
    public string[] Templates { get; set; } = [];
    [CommandOption("-n|--name <NAME>")]
    public string Name { get; set; } = "";
    [CommandOption("-o|--output <PATH>")]
    public string? Output { get; set; }
    [CommandOption("--non-interactive")]
    public bool NonInteractive { get; set; }
    [CommandOption("--json")]
    public bool Json { get; set; }
    [CommandOption("--db-server <SERVER>")]
    public string? DbServer { get; set; }
    [CommandOption("--db-name <NAME>")]
    public string? DbName { get; set; }
    [CommandOption("--db-user <USER>")]
    public string? DbUser { get; set; }
    [CommandOption("--db-password <PASSWORD>")]
    [Description("Grava somente em User Secrets; prefira configurar segredos fora da linha de comando")]
    public string? DbPassword { get; set; }
    [CommandOption("--mediatr-license <LICENSE>")]
    public string? MediatrLicense { get; set; }
    [CommandOption("--automapper-license <LICENSE>")]
    public string? AutomapperLicense { get; set; }

    public string? SelectDatabase()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new CliException("INPUT_REQUIRED", "Informe --name.");
        if (!Identifiers.IsName(Name)) throw new CliException("NAME_INVALID", "Use um nome C# válido, sem palavras reservadas.");
        if (!Type.Equals("api", StringComparison.OrdinalIgnoreCase)) throw new CliException("TYPE_UNSUPPORTED", "Somente --type api é suportado.");
        var values = Databases.Concat(Templates).Select(Identifiers.Database).Distinct().ToArray();
        if (values.Length > 1) throw new CliException("ARGUMENT_CONFLICT", "As opções de banco devem indicar o mesmo provider.");
        return values.FirstOrDefault();
    }
}

public class NewCommand(IDotNetRunner runner, IAnsiConsole console) : AsyncCommand<NewSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, NewSettings settings, CancellationToken cancellationToken)
    {
        var warnings = new List<Notice>();
        string? root = null;
        var stage = "validation";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var database = settings.SelectDatabase();
            if (settings.Templates.Length > 0) warnings.Add(new("DEPRECATED_OPTION", "Use --database em vez de --template."));
            if (settings.MediatrLicense is not null || settings.AutomapperLicense is not null)
                warnings.Add(new("IGNORED_LICENSE", "O template único não utiliza as opções de licença legadas."));
            if (database is null)
            {
                if (settings.Json || settings.NonInteractive || !console.Profile.Capabilities.Interactive)
                    throw new CliException("INPUT_REQUIRED", "Informe --database postgres|sqlserver|oracle.");
                database = await console.PromptAsync(new SelectionPrompt<string>().Title("Banco de dados:").AddChoices("postgres", "sqlserver", "oracle"), cancellationToken);
            }
            root = Path.GetFullPath(settings.Output ?? settings.Name);
            if (File.Exists(root) || Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
                throw new CliException("DESTINATION_NOT_EMPTY", "O destino deve ser uma pasta vazia ou inexistente.");
            if (!runner.IsSdkVersionSufficient(10)) throw new CliException("SDK_INCOMPATIBLE", "Instale um SDK .NET 10 estável ou superior.", 3);
            var version = await runner.GetInstalledTemplateVersionAsync(PackageIds.Unified, cancellationToken);
            if (version is null) throw new CliException("TEMPLATE_NOT_INSTALLED", "Execute openbase install para instalar o template compatível.", 3);
            if (!new PackageVersion(version).Compatible) throw new CliException("TEMPLATE_VERSION_UNSUPPORTED", "Este CLI requer o template 11.x (manifesto v2).", 3);
            var probe = await runner.RunAsync(new[] { "new", "openbasenet", "--name", settings.Name, "--output", root, "--database", database, "--dry-run" }, cancellationToken);
            if (!probe.Success) throw new CliException("TEMPLATE_CAPABILITY_UNAVAILABLE", "O template instalado não aceita o contrato de criação.", 3);
            cancellationToken.ThrowIfCancellationRequested();
            stage = "generation";
            var result = await runner.RunAsync(new[] { "new", "openbasenet", "--name", settings.Name, "--output", root, "--database", database }, cancellationToken);
            if (!result.Success) throw new CliException("GENERATION_FAILED", "Falha na geração. Arquivos parciais foram preservados.", 4);
            stage = "manifest";
            var manifest = GeneratedManifest.Read(root, database, settings.Name, version);
            if (settings.DbServer is not null || settings.DbName is not null || settings.DbUser is not null || settings.DbPassword is not null)
            {
                stage = "configuration";
                var secret = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [$"ConnectionStrings:{manifest.ConnectionKey}"] = BuildConnectionString(database, settings)
                });
                // dotnet user-secrets accepts a JSON dictionary on stdin: credentials never enter child argv or logs.
                var config = await runner.RunAsync(new[] { "user-secrets", "set", "--project", manifest.ApiProject }, cancellationToken, secret);
                if (!config.Success) throw new CliException("CONFIGURATION_FAILED", "Projeto gerado; configuração de User Secrets falhou.", 4);
            }
            return CommandResult.Write(console, settings.Json, "new", new { projectRoot = root, manifestPath = Path.Combine(root, ".openbase.json") }, warnings: warnings);
        }
        catch (OperationCanceledException)
        { return Fail(new("CANCELLED", "Operação cancelada.", 130)); }
        catch (CliException e) { return Fail(e); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception)
        { return Fail(new("CREATION_FAILED", "Não foi possível concluir a criação.", 4)); }

        int Fail(CliException error)
        {
            if (stage != "validation") warnings.Add(new("PARTIAL_OUTPUT", $"Pasta: {root}. Etapa interrompida: {stage}. Arquivos preservados."));
            return CommandResult.Write(console, settings.Json, "new", error: error, warnings: warnings);
        }
    }

    public static string BuildConnectionString(string database, NewSettings settings)
    {
        DbConnectionStringBuilder builder = database switch
        {
            "postgres" => new Npgsql.NpgsqlConnectionStringBuilder { Host = settings.DbServer ?? "localhost", Database = settings.DbName ?? settings.Name, Username = settings.DbUser ?? "", Password = settings.DbPassword ?? "" },
            "sqlserver" => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder { DataSource = settings.DbServer ?? ".", InitialCatalog = settings.DbName ?? settings.Name, UserID = settings.DbUser ?? "", Password = settings.DbPassword ?? "", IntegratedSecurity = string.IsNullOrEmpty(settings.DbUser), TrustServerCertificate = true },
            _ => new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder { DataSource = (settings.DbServer ?? "localhost:1521").Contains('/') ? settings.DbServer : $"{settings.DbServer ?? "localhost:1521"}/{settings.DbName ?? settings.Name}", UserID = settings.DbUser ?? "", Password = settings.DbPassword ?? "" }
        };
        return builder.ConnectionString;
    }
}
