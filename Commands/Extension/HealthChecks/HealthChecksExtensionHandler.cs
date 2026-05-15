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
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, _, infraDataPath, presentationPath) = paths.Value;

        var detected = DetectServices(context, ns, infraDataPath);

        AddNuGetPackages(ns, presentationPath, detected);
        ExtensionHelpers.WriteFiles(GetFiles(ns, presentationPath, detected), solutionDir, fileWriter, console);
        InjectProgramCs(ns, presentationPath);

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

        var packages = new List<string>
        {
            "AspNetCore.HealthChecks.UI.Client",
        };

        if (detected.HasSqlServer) packages.Add("AspNetCore.HealthChecks.SqlServer");
        if (detected.HasPostgres) packages.Add("AspNetCore.HealthChecks.NpgSql");
        if (detected.HasRedis) packages.Add("AspNetCore.HealthChecks.Redis");
        if (detected.HasRabbitMq) packages.Add("AspNetCore.HealthChecks.RabbitMQ");

        foreach (var packageId in packages)
            ExtensionHelpers.AddPackage(presentationCsproj, packageId, fileWriter, dotNetRunner, console);
    }

    private void InjectProgramCs(string ns, string presentationPath) =>
        ExtensionHelpers.InjectProgramCs(
            presentationPath, fileWriter, console,
            new ProgramCsMessages(
                SR.Current.HealthChecksProgramCsNotFound,
                SR.Current.HealthChecksProgramCsAlreadyConfigured,
                SR.Current.HealthChecksProgramCsInjected,
                SR.Current.HealthChecksProgramCsWarning),
            content => IsProgramCsAlreadyConfigured(content, ns),
            content => InjectMapHealthChecks(InjectAddHealthChecks(
                ExtensionHelpers.InjectPresentationUsing(content, ns))));

    private static bool IsProgramCsAlreadyConfigured(string content, string ns) =>
        content.Contains("AddOpenBaseHealthChecks") &&
        content.Contains("MapOpenBaseHealthChecks") &&
        content.Contains($"using {ns}.Presentation.Api.Extensions;");

    private static string InjectAddHealthChecks(string content)
    {
        const string call = "builder.Services.AddOpenBaseHealthChecks(builder.Configuration);";
        if (content.Contains(call)) return content;
        return ExtensionHelpers.InsertBeforeAnchor(content, "var app = builder.Build();", $"{call}\n");
    }

    private static string InjectMapHealthChecks(string content)
    {
        const string call = "app.MapOpenBaseHealthChecks();";
        if (content.Contains(call)) return content;
        return ExtensionHelpers.InsertBeforeAnchor(content, "app.MapControllers();", $"{call}\n");
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
