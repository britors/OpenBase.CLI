using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension.HealthChecks;

public sealed class HealthChecksExtensionHandler(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IFileWriter fileWriter,
    IExtensionRegistry extensionRegistry) : IExtensionHandler
{
    public string Name => "healthchecks";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        if (context.SolutionDir is null || context.RootNamespace is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var ns = context.RootNamespace;
        var src = Path.Combine(context.SolutionDir, "src");
        var infraDataPath = Path.Combine(src, $"{ns}.Infra.Data");
        var presentationPath = Path.Combine(src, $"{ns}.Presentation.Api");

        var detected = DetectServices(context, ns, infraDataPath);

        AddNuGetPackages(ns, presentationPath, detected);
        CreateFiles(ns, presentationPath, context.SolutionDir, detected);
        InjectProgramCs(presentationPath);

        return new ExtensionApplyResult(true);
    }

    private DetectedServices DetectServices(ExtensionContext context, string ns, string infraDataPath)
    {
        var infraCsproj = Path.Combine(infraDataPath, $"{ns}.Infra.Data.csproj");
        var csprojContent = fileWriter.FileExists(infraCsproj)
            ? fileWriter.ReadAllText(infraCsproj)
            : string.Empty;

        var installed = extensionRegistry.GetAll(context.ProjectDir);

        return new DetectedServices(
            HasSqlServer: csprojContent.Contains("EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase),
            HasPostgres: csprojContent.Contains("Npgsql.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase),
            HasRedis: installed.Any(e => string.Equals(e.Name, "redis", StringComparison.OrdinalIgnoreCase)),
            HasRabbitMq: installed.Any(e => string.Equals(e.Name, "rabbitmq", StringComparison.OrdinalIgnoreCase))
        );
    }

    private void AddNuGetPackages(string ns, string presentationPath, DetectedServices detected)
    {
        var presentationCsproj = Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj");
        if (!fileWriter.FileExists(presentationCsproj)) return;

        var csprojContent = fileWriter.ReadAllText(presentationCsproj);

        var packages = new List<string>
        {
            "AspNetCore.HealthChecks.UI",
            "AspNetCore.HealthChecks.UI.Client",
            "AspNetCore.HealthChecks.UI.InMemory.Storage",
        };

        if (detected.HasSqlServer) packages.Add("AspNetCore.HealthChecks.SqlServer");
        if (detected.HasPostgres) packages.Add("AspNetCore.HealthChecks.NpgSql");
        if (detected.HasRedis) packages.Add("AspNetCore.HealthChecks.Redis");
        if (detected.HasRabbitMq) packages.Add("AspNetCore.HealthChecks.RabbitMQ");

        foreach (var packageId in packages)
        {
            if (csprojContent.Contains(packageId)) continue;

            console.MarkupLine(string.Format(SR.Current.ExtensionAddingPackage, packageId, Path.GetFileName(presentationCsproj)));
            var (ok, err) = dotNetRunner.Run($"add \"{presentationCsproj}\" package {packageId}");
            if (!ok)
                console.MarkupLine(string.Format(SR.Current.ExtensionPackageAddWarning, packageId, err));
        }
    }

    private void CreateFiles(string ns, string presentationPath, string solutionDir, DetectedServices detected)
    {
        foreach (var (path, content) in GetFiles(ns, presentationPath, detected))
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

    private void InjectProgramCs(string presentationPath)
    {
        var path = Path.Combine(presentationPath, "Program.cs");
        if (!fileWriter.FileExists(path))
        {
            console.MarkupLine(SR.Current.HealthChecksProgramCsNotFound);
            return;
        }

        try
        {
            var content = fileWriter.ReadAllText(path);
            if (IsProgramCsAlreadyConfigured(content))
            {
                console.MarkupLine(SR.Current.HealthChecksProgramCsAlreadyConfigured);
                return;
            }

            content = InjectAddHealthChecks(content);
            content = InjectMapHealthChecks(content);
            fileWriter.WriteAllText(path, content);
            console.MarkupLine(SR.Current.HealthChecksProgramCsInjected);
        }
        catch (Exception ex)
        {
            console.MarkupLine(string.Format(SR.Current.HealthChecksProgramCsWarning, ex.Message));
        }
    }

    private static bool IsProgramCsAlreadyConfigured(string content) =>
        content.Contains("AddOpenBaseHealthChecks") &&
        content.Contains("MapOpenBaseHealthChecks");

    private static string InjectAddHealthChecks(string content)
    {
        const string call = "builder.Services.AddOpenBaseHealthChecks(builder.Configuration);";
        if (content.Contains(call)) return content;

        const string anchor = "var app = builder.Build();";
        var idx = content.IndexOf(anchor, StringComparison.Ordinal);
        return idx >= 0 ? content.Insert(idx, $"{call}\n") : content;
    }

    private static string InjectMapHealthChecks(string content)
    {
        const string call = "app.MapOpenBaseHealthChecks();";
        if (content.Contains(call)) return content;

        const string anchor = "app.MapControllers();";
        var idx = content.IndexOf(anchor, StringComparison.Ordinal);
        return idx >= 0 ? content.Insert(idx, $"{call}\n") : content;
    }

    public static IEnumerable<(string Path, string Content)> GetFiles(
        string ns, string presentationPath, DetectedServices detected)
    {
        yield return (
            Path.Combine(presentationPath, "Extensions", "HealthChecksExtensions.cs"),
            HealthChecksExtensionsTemplate(ns, detected));
    }

    private static string HealthChecksExtensionsTemplate(string ns, DetectedServices detected)
    {
        var checksPart = BuildChecksPart(detected);

        return $$"""
            using HealthChecks.UI.Client;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Diagnostics.HealthChecks;
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{ns}}.Presentation.Api.Extensions;

            public static class HealthChecksExtensions
            {
                public static IServiceCollection AddOpenBaseHealthChecks(
                    this IServiceCollection services, IConfiguration configuration)
                {
                    services.AddHealthChecks(){{checksPart}};

                    services.AddHealthChecksUI(opt =>
                        {
                            opt.SetEvaluationTimeInSeconds(60);
                            opt.AddHealthCheckEndpoint("API", "/health");
                        })
                        .AddInMemoryStorage();

                    return services;
                }

                public static IEndpointRouteBuilder MapOpenBaseHealthChecks(
                    this IEndpointRouteBuilder app)
                {
                    app.MapHealthChecks("/health", new HealthCheckOptions
                    {
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });

                    app.MapHealthChecks("/health/ready", new HealthCheckOptions
                    {
                        Predicate = hc => hc.Tags.Contains("ready"),
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });

                    app.MapHealthChecksUI(opt => opt.UIPath = "/health-ui");

                    return app;
                }
            }
            """;
    }

    private static string BuildChecksPart(DetectedServices detected)
    {
        var lines = new List<string>();

        if (detected.HasSqlServer)
            lines.Add("            .AddSqlServer(configuration[\"ConnectionStrings:DefaultConnection\"]!, name: \"sqlserver\", tags: [\"ready\"])");
        if (detected.HasPostgres)
            lines.Add("            .AddNpgSql(configuration[\"ConnectionStrings:DefaultConnection\"]!, name: \"postgres\", tags: [\"ready\"])");
        if (detected.HasRedis)
            lines.Add("            .AddRedis(configuration[\"Redis:ConnectionString\"]!, name: \"redis\", tags: [\"ready\"])");
        if (detected.HasRabbitMq)
            lines.Add("            .AddRabbitMQ(rabbitConnectionString: configuration[\"RabbitMQ:ConnectionString\"]!, name: \"rabbitmq\", tags: [\"ready\"])");

        return lines.Count == 0 ? string.Empty : "\n" + string.Join("\n", lines);
    }
}

public record DetectedServices(bool HasSqlServer, bool HasPostgres, bool HasRedis, bool HasRabbitMq);
