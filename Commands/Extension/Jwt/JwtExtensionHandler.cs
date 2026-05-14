using System.Text.Json;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension.Jwt;

public sealed class JwtExtensionHandler(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IFileWriter fileWriter) : IExtensionHandler
{
    private const string PackageId = "Microsoft.AspNetCore.Authentication.JwtBearer";

    public string Name => "jwt";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext ctx)
    {
        if (ctx.SolutionDir is null || ctx.RootNamespace is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var ns = ctx.RootNamespace;
        var src = Path.Combine(ctx.SolutionDir, "src");
        var appPath = Path.Combine(src, $"{ns}.Application");
        var infraDataPath = Path.Combine(src, $"{ns}.Infra.Data");
        var presentationPath = Path.Combine(src, $"{ns}.Presentation.Api");

        AddNuGetPackages(ns, infraDataPath, presentationPath);
        CreateFiles(ns, appPath, infraDataPath, presentationPath, ctx.SolutionDir);
        InjectAppSettings(presentationPath, ns);

        console.MarkupLine(SR.Current.JwtNextSteps);
        console.MarkupLine(SR.Current.JwtNextStep1);
        console.MarkupLine(SR.Current.JwtNextStep2);
        console.MarkupLine(SR.Current.JwtNextStep3);

        return new ExtensionApplyResult(true);
    }

    private void AddNuGetPackages(string ns, string infraDataPath, string presentationPath)
    {
        var targets = new[]
        {
            Path.Combine(infraDataPath, $"{ns}.Infra.Data.csproj"),
            Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj"),
        };

        foreach (var csproj in targets)
        {
            if (!fileWriter.FileExists(csproj)) continue;

            var content = fileWriter.ReadAllText(csproj);
            if (content.Contains(PackageId)) continue;

            console.MarkupLine(string.Format(SR.Current.ExtensionAddingPackage, PackageId, Path.GetFileName(csproj)));
            var (ok, err) = dotNetRunner.Run($"add \"{csproj}\" package {PackageId}");
            if (!ok)
                console.MarkupLine(string.Format(SR.Current.ExtensionPackageAddWarning, PackageId, err));
        }
    }

    private void CreateFiles(string ns, string appPath, string infraDataPath, string presentationPath, string solutionDir)
    {
        foreach (var (path, content) in GetFiles(ns, appPath, infraDataPath, presentationPath))
        {
            if (fileWriter.FileExists(path))
            {
                console.MarkupLine(string.Format(SR.Current.ExtensionFileSkipped, Path.GetFileName(path)));
                continue;
            }
            fileWriter.EnsureDirectory(Path.GetDirectoryName(path)!);
            fileWriter.WriteAllText(path, content);
            console.MarkupLine(string.Format(SR.Current.ExtensionFileCreated, Path.GetRelativePath(solutionDir, path)));
        }
    }

    private void InjectAppSettings(string presentationPath, string ns)
    {
        var path = Path.Combine(presentationPath, "appsettings.json");
        if (!fileWriter.FileExists(path)) return;

        try
        {
            var json = fileWriter.ReadAllText(path);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null || root.ContainsKey("Jwt")) return;

            root["Jwt"] = new JsonObject
            {
                ["Secret"] = "CHANGE-ME-USE-AT-LEAST-32-CHARS-SECRET",
                ["Issuer"] = ns,
                ["Audience"] = ns,
                ["ExpirationMinutes"] = 60
            };

            fileWriter.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            console.MarkupLine(SR.Current.JwtAppSettingsInjected);
        }
        catch (Exception ex)
        {
            console.MarkupLine(string.Format(SR.Current.JwtAppSettingsWarning, ex.Message));
        }
    }

    public static IEnumerable<(string Path, string Content)> GetFiles(
        string ns, string appPath, string infraDataPath, string presentationPath)
    {
        yield return (
            Path.Combine(appPath, "Interfaces", "Services", "ITokenService.cs"),
            ITokenServiceTemplate(ns));

        yield return (
            Path.Combine(infraDataPath, "Services", "TokenService.cs"),
            TokenServiceTemplate(ns));

        yield return (
            Path.Combine(presentationPath, "Extensions", "JwtExtensions.cs"),
            JwtExtensionsTemplate(ns));
    }

    private static string ITokenServiceTemplate(string ns) => $$"""
        namespace {{ns}}.Application.Interfaces.Services;

        public interface ITokenService
        {
            string GenerateToken(int userId, string email, IEnumerable<string>? roles = null);
        }
        """;

    private static string TokenServiceTemplate(string ns) => $$"""
        using System.IdentityModel.Tokens.Jwt;
        using System.Security.Claims;
        using System.Text;
        using Microsoft.Extensions.Configuration;
        using Microsoft.IdentityModel.Tokens;
        using {{ns}}.Application.Interfaces.Services;

        namespace {{ns}}.Infra.Data.Services;

        public sealed class TokenService(IConfiguration configuration) : ITokenService
        {
            public string GenerateToken(int userId, string email, IEnumerable<string>? roles = null)
            {
                var secret = configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret not configured.");
                var issuer = configuration["Jwt:Issuer"];
                var audience = configuration["Jwt:Audience"];
                var expMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var m) ? m : 60;

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Email, email),
                };

                if (roles is not null)
                    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expMinutes),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
        }
        """;

    private static string JwtExtensionsTemplate(string ns) => $$"""
        using System.Text;
        using Microsoft.AspNetCore.Authentication.JwtBearer;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.IdentityModel.Tokens;

        namespace {{ns}}.Presentation.Api.Extensions;

        public static class JwtExtensions
        {
            public static IServiceCollection AddJwtAuthentication(
                this IServiceCollection services, IConfiguration configuration)
            {
                var secret = configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret not configured.");
                var issuer = configuration["Jwt:Issuer"];
                var audience = configuration["Jwt:Audience"];

                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = issuer,
                            ValidAudience = audience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(secret))
                        };
                    });

                services.AddAuthorization();

                return services;
            }
        }
        """;
}
