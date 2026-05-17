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

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, infraDataPath, presentationPath) = paths.Value;

        AddNuGetPackages(ns, infraDataPath, presentationPath);
        AddProjectReferences(ns, appPath, infraDataPath);
        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraDataPath, presentationPath), solutionDir, fileWriter, console);
        InjectAppSettings(presentationPath, ns);
        InjectProgramCs(presentationPath, ns);
        ProtectExistingControllers(presentationPath, solutionDir);

        return new ExtensionApplyResult(true);
    }

    private void AddProjectReferences(string ns, string appPath, string infraDataPath)
    {
        var infraDataCsproj = Path.Combine(infraDataPath, $"{ns}.Infra.Data.csproj");
        var appCsproj = Path.Combine(appPath, $"{ns}.Application.csproj");
        ExtensionHelpers.AddProjectReference(infraDataCsproj, appCsproj, fileWriter, dotNetRunner, console);
    }

    private void AddNuGetPackages(string ns, string infraDataPath, string presentationPath)
    {
        foreach (var csproj in new[]
        {
            Path.Combine(infraDataPath, $"{ns}.Infra.Data.csproj"),
            Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj"),
        })
            ExtensionHelpers.AddPackage(csproj, PackageId, fileWriter, dotNetRunner, console);
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

    private void InjectProgramCs(string presentationPath, string ns)
    {
        var path = Path.Combine(presentationPath, "Program.cs");
        if (!fileWriter.FileExists(path))
        {
            console.MarkupLine(SR.Current.JwtProgramCsNotFound);
            return;
        }

        try
        {
            var content = fileWriter.ReadAllText(path);
            if (IsProgramCsAlreadyConfigured(content, ns))
            {
                console.MarkupLine(SR.Current.JwtProgramCsAlreadyConfigured);
                return;
            }

            content = ExtensionHelpers.InjectPresentationUsing(content, ns);
            content = InjectAddJwt(content);
            content = InjectUseAuthMiddleware(content);
            content = InjectSwaggerJwtSupport(content);
            fileWriter.WriteAllText(path, content);
            console.MarkupLine(SR.Current.JwtProgramCsInjected);
            console.MarkupLine(SR.Current.JwtSwaggerInjected);
        }
        catch (Exception ex)
        {
            console.MarkupLine(string.Format(SR.Current.JwtProgramCsWarning, ex.Message));
        }
    }

    private static bool IsProgramCsAlreadyConfigured(string content, string ns) =>
        content.Contains("builder.Services.AddJwtAuthentication(builder.Configuration);")
        && content.Contains("app.UseAuthentication();")
        && content.Contains("app.UseAuthorization();")
        && content.Contains($"using {ns}.Presentation.Api.Extensions;")
        && content.Contains("builder.Services.AddSwaggerJwtSupport();");

    private static string InjectSwaggerJwtSupport(string content)
    {
        const string swaggerJwtCall = "builder.Services.AddSwaggerJwtSupport();";
        if (content.Contains(swaggerJwtCall)) return content;

        const string swaggerGenAnchor = "builder.Services.AddSwaggerGen();";
        if (content.Contains(swaggerGenAnchor))
        {
            var idx = content.IndexOf(swaggerGenAnchor, StringComparison.Ordinal) + swaggerGenAnchor.Length;
            var afterLine = ExtensionHelpers.SkipNewLine(content, idx);
            return content.Insert(afterLine, $"{swaggerJwtCall}\n");
        }

        return ExtensionHelpers.InsertBeforeAnchor(content, "var app = builder.Build();", $"{swaggerJwtCall}\n");
    }

    private static string InjectAddJwt(string content)
    {
        const string addJwtCall = "builder.Services.AddJwtAuthentication(builder.Configuration);";
        if (content.Contains(addJwtCall)) return content;
        return ExtensionHelpers.InsertBeforeAnchor(content, "var app = builder.Build();", $"{addJwtCall}\n");
    }

    private static string InjectUseAuthMiddleware(string content)
    {
        const string useAuthCall = "app.UseAuthentication();";
        const string useAuthzCall = "app.UseAuthorization();";
        const string mapAnchor = "app.MapControllers();";

        var needsAuth = !content.Contains(useAuthCall);
        var needsAuthz = !content.Contains(useAuthzCall);

        if (!needsAuth && !needsAuthz) return content;

        if (!needsAuth)
            return ExtensionHelpers.InsertAfterLine(content, useAuthCall, useAuthzCall);

        var injection = $"{useAuthCall}\n{(needsAuthz ? useAuthzCall + "\n" : string.Empty)}";
        return ExtensionHelpers.InsertBeforeAnchor(content, mapAnchor, injection);
    }

    private void ProtectExistingControllers(string presentationPath, string solutionDir)
    {
        var controllersDir = Path.Combine(presentationPath, "Controllers");
        var files = fileWriter.GetFiles(controllersDir, "*Controller.cs");

        foreach (var filePath in files)
            ProtectController(filePath, solutionDir);
    }

    private void ProtectController(string filePath, string solutionDir)
    {
        var content = fileWriter.ReadAllText(filePath);

        if (!content.Contains("ControllerBase") || content.Contains("[Authorize]"))
            return;

        content = InjectAuthorizationUsing(content);
        content = InjectAuthorizeAttribute(content);

        fileWriter.WriteAllText(filePath, content);
        console.MarkupLine(string.Format(SR.Current.JwtControllerProtected,
            Path.GetRelativePath(solutionDir, filePath)));
    }

    private static string InjectAuthorizationUsing(string content)
    {
        const string authUsing = "using Microsoft.AspNetCore.Authorization;";
        if (content.Contains(authUsing)) return content;

        const string mvcUsing = "using Microsoft.AspNetCore.Mvc;";
        var usingIdx = content.IndexOf(mvcUsing, StringComparison.Ordinal);
        if (usingIdx < 0) return content;

        var afterLine = ExtensionHelpers.SkipNewLine(content, usingIdx + mvcUsing.Length);
        return content.Insert(afterLine, $"{authUsing}\n");
    }

    private static string InjectAuthorizeAttribute(string content)
    {
        const string apiControllerAttr = "[ApiController]";
        var apiCtrlIdx = content.IndexOf(apiControllerAttr, StringComparison.Ordinal);
        if (apiCtrlIdx < 0) return content;

        var afterAttr = ExtensionHelpers.SkipNewLine(content, apiCtrlIdx + apiControllerAttr.Length);
        return content.Insert(afterAttr, "[Authorize]\n");
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
        using Microsoft.OpenApi;

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

            public static IServiceCollection AddSwaggerJwtSupport(this IServiceCollection services)
            {
                services.AddSwaggerGen(options =>
                {
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Informe o token JWT."
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            []
                        }
                    });
                });

                return services;
            }
        }
        """;
}
