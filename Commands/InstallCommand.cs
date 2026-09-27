using System.Text.Json;
using System.ComponentModel;
using System.IO.Compression;
using System.Xml.Linq;
using OpenBase.CLI.Helpers.Creation;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class InstallSettings : CommandSettings
{
    [CommandOption("--type <COMPONENT>")]
    [Description("template (padrão), cli, ou pacote legado: postgres, sqlserver, oracle")]
    public string Type { get; set; } = "template";
    [CommandOption("--version <VERSION>")]
    public string? Version { get; set; }
    [CommandOption("--prerelease")]
    public bool Prerelease { get; set; }
    [CommandOption("--package <NUPKG>")]
    [Description("Instala explicitamente um .nupkg local do template único")]
    public string? Package { get; set; }
    [CommandOption("--json")]
    public bool Json { get; set; }
    [CommandOption("--non-interactive")]
    public bool NonInteractive { get; set; }
}

public class InstallCommand(PackageOperations operations) : AsyncCommand<InstallSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, InstallSettings settings, CancellationToken token)
        => operations.RunAsync("install", settings, token);
}

public sealed class PackageOperations(IDotNetRunner runner, IPackageCatalog catalog, IUpdateHistoryService history, IAnsiConsole console)
{
    public async Task<int> RunAsync(string command, InstallSettings settings, CancellationToken token)
    {
        var warnings = new List<Notice> { new("TEMPLATE_ONLY", "Alterar o pacote não migra o código nem o banco de aplicações existentes.") };
        try
        {
            token.ThrowIfCancellationRequested();
            if (!PackageIds.TypeToId.TryGetValue(settings.Type, out var packageId)) throw new CliException("COMPONENT_UNSUPPORTED", "Use template, cli, postgres, sqlserver ou oracle.");
            if (!runner.IsSdkVersionSufficient(10)) throw new CliException("SDK_INCOMPATIBLE", "Instale um SDK .NET 10 estável ou superior.", 3);
            string version;
            string? local = null;
            if (settings.Package is not null)
            {
                if (packageId != PackageIds.Unified || settings.Version is not null || settings.Prerelease)
                    throw new CliException("ARGUMENT_CONFLICT", "--package é exclusivo do template único; não combine com --version/--prerelease.");
                local = Path.GetFullPath(settings.Package);
                using var zip = ZipFile.OpenRead(local);
                var nuspec = zip.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
                using var stream = nuspec.Open();
                var metadata = XDocument.Load(stream).Descendants().Single(e => e.Name.LocalName == "metadata");
                var id = metadata.Elements().Single(e => e.Name.LocalName == "id").Value;
                version = metadata.Elements().Single(e => e.Name.LocalName == "version").Value;
                if (!id.Equals(PackageIds.Unified, StringComparison.OrdinalIgnoreCase) || !new PackageVersion(version).Compatible)
                    throw new CliException("PACKAGE_VERSION_UNAVAILABLE", "O pacote local deve ser w3ti.OpenBaseNET.Template 11.x.", 3);
            }
            else
                version = PackageCatalog.Select(await catalog.VersionsAsync(packageId, token), packageId, settings.Version, settings.Prerelease);
            var previous = packageId == PackageIds.Cli
                ? await runner.GetInstalledToolVersionAsync(packageId, token)
                : await runner.GetInstalledTemplateVersionAsync(packageId, token);
            // update never silently downgrades; version restore is the explicit downgrade operation.
            if (command == "update" && PackageVersion.IsValid(previous) && new PackageVersion(version).CompareTo(new(previous!)) < 0)
                throw new CliException("VERSION_DOWNGRADE", "Use version restore para voltar a uma versão anterior.", 3);
            IReadOnlyList<string> args = packageId == PackageIds.Cli
                ? new[] { "tool", "update", "-g", packageId, "--version", version, "--allow-downgrade" }
                : new[] { "new", "install", local ?? $"{packageId}::{version}", "--force" };
            var result = await runner.RunAsync(args, token);
            var actual = result.Success ? packageId == PackageIds.Cli
                ? await runner.GetInstalledToolVersionAsync(packageId, token)
                : await runner.GetInstalledTemplateVersionAsync(packageId, token) : null;
            var succeeded = result.Success && PackageVersion.IsValid(actual) && new PackageVersion(actual!).CompareTo(new(version)) == 0;
            await history.AddEntryAsync(new UpdateHistoryEntry { Date = DateTime.UtcNow, Component = packageId, PreviousVersion = previous, NewVersion = actual, Success = succeeded }, token);
            if (!succeeded) throw new CliException("PACKAGE_OPERATION_FAILED", "Não foi possível instalar e confirmar a versão solicitada.", 4);
            return CommandResult.Write(console, settings.Json, command, new { packageId, previousVersion = previous, version = actual }, warnings: warnings);
        }
        catch (CliException e) { return Fail(e); }
        catch (OperationCanceledException) { return Fail(token.IsCancellationRequested ? new("CANCELLED", "Operação cancelada.", 130) : new("PACKAGE_QUERY_FAILED", "Tempo esgotado ao consultar versões.", 3)); }
        catch (HttpRequestException) { return Fail(new("PACKAGE_QUERY_FAILED", "Falha ao consultar versões do NuGet.", 3)); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Xml.XmlException or JsonException or System.ComponentModel.Win32Exception)
        { return Fail(new("PACKAGE_OPERATION_FAILED", "Não foi possível concluir a operação do pacote.", 4)); }
        int Fail(CliException e) => CommandResult.Write(console, settings.Json, command, error: e, warnings: warnings);
    }
}
