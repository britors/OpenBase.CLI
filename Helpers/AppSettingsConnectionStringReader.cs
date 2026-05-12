using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class AppSettingsConnectionStringReader : IConnectionStringReader
{
    public string? Read(string solutionDir, string rootNamespace)
    {
        var presentationPath = Path.Combine(solutionDir, "src", $"{rootNamespace}.Presentation.Api");

        var candidates = new[]
        {
            Path.Combine(presentationPath, "appsettings.Development.json"),
            Path.Combine(presentationPath, "appsettings.json"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;

            try
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs))
                {
                    foreach (var prop in cs.EnumerateObject())
                    {
                        var value = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
            }
            catch { /* arquivo inválido ou sem a chave */ }
        }

        return null;
    }
}
