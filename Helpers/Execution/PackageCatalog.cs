using System.Net;
using System.Text.Json;
using OpenBase.CLI.Helpers.Creation;

namespace OpenBase.CLI.Helpers.Execution;

public interface IPackageCatalog
{
    Task<IReadOnlyList<string>> VersionsAsync(string packageId, CancellationToken token);
}

public sealed class PackageCatalog : IPackageCatalog
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(45) };
    public async Task<IReadOnlyList<string>> VersionsAsync(string packageId, CancellationToken token)
    {
        using var response = await Client.GetAsync($"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json", token);
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        return json.RootElement.GetProperty("versions").EnumerateArray().Select(v => v.GetString()!).ToArray();
    }

    public static string Select(IEnumerable<string> available, string package, string? requested, bool preview)
    {
        if (requested is not null && !PackageVersion.IsValid(requested)) throw new CliException("VERSION_INVALID", "Informe uma versão SemVer exata.");
        var versions = available.Where(PackageVersion.IsValid).Select(v => new PackageVersion(v))
            .Where(v => package != PackageIds.Unified || v.Compatible)
            .Where(v => requested is not null ? v.CompareTo(new PackageVersion(requested)) == 0 : preview || !v.Preview)
            .OrderDescending().ToArray();
        return versions.FirstOrDefault()?.Value ?? throw new CliException("PACKAGE_VERSION_UNAVAILABLE", "Nenhuma versão compatível disponível. Use --prerelease para previews ou --package para um pacote local.", 3);
    }
}
