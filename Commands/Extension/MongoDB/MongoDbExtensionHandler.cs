using System.Text.Json;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension.MongoDB;

public sealed class MongoDbExtensionHandler(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IFileWriter fileWriter) : IExtensionHandler
{
    private const string MongoDriverPackageId    = "MongoDB.Driver";
    private const string ResiliencePackageId     = "Microsoft.Extensions.Resilience";

    public string Name => "mongodb";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, _, presentationPath) = paths.Value;
        var infraMongoPath = Path.Combine(solutionDir, "src", $"{ns}.Infra.MongoDb");

        CreateInfraMongoDbProject(ns, solutionDir, infraMongoPath, appPath, presentationPath);
        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraMongoPath, presentationPath), solutionDir, fileWriter, console);
        InjectAppSettings(presentationPath);
        InjectProgramCs(ns, presentationPath);

        return new ExtensionApplyResult(true);
    }

    private void CreateInfraMongoDbProject(
        string ns, string solutionDir, string infraMongoPath, string appPath, string presentationPath)
    {
        var infraMongoCsproj    = Path.Combine(infraMongoPath, $"{ns}.Infra.MongoDb.csproj");
        var appCsproj           = Path.Combine(appPath, $"{ns}.Application.csproj");
        var presentationCsproj  = Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj");

        if (!fileWriter.FileExists(infraMongoCsproj))
        {
            var (ok, err) = dotNetRunner.Run($"new classlib -n \"{ns}.Infra.MongoDb\" -o \"{infraMongoPath}\"");
            if (!ok)
            {
                console.MarkupLine(string.Format(SR.Current.InfraMongoDbProjectFailed, $"{ns}.Infra.MongoDb", err));
                return;
            }

            var slnFile = fileWriter.FindSolutionFile(solutionDir);
            if (slnFile is not null)
                dotNetRunner.Run($"sln \"{slnFile}\" add \"{infraMongoCsproj}\"");

            console.MarkupLine(string.Format(SR.Current.InfraMongoDbProjectCreated, $"{ns}.Infra.MongoDb"));
        }
        else
        {
            console.MarkupLine(string.Format(SR.Current.InfraMongoDbProjectAlreadyExists, $"{ns}.Infra.MongoDb"));
        }

        ExtensionHelpers.AddProjectReference(infraMongoCsproj, appCsproj, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddProjectReference(presentationCsproj, infraMongoCsproj, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddPackage(infraMongoCsproj, MongoDriverPackageId, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddPackage(infraMongoCsproj, ResiliencePackageId, fileWriter, dotNetRunner, console);
    }

    private void InjectAppSettings(string presentationPath)
    {
        string[] candidates = ["appsettings.json", "appsettings.Development.json"];

        foreach (var fileName in candidates)
        {
            var path = Path.Combine(presentationPath, fileName);
            if (!fileWriter.FileExists(path)) continue;

            try
            {
                var json = fileWriter.ReadAllText(path);
                var root = JsonNode.Parse(json)?.AsObject();
                if (root is null || root.ContainsKey("MongoDb")) continue;

                root["MongoDb"] = new JsonObject
                {
                    ["ConnectionString"] = "mongodb://localhost:27017",
                    ["DatabaseName"]     = "openbase",
                    ["Retry"] = new JsonObject
                    {
                        ["MaxAttempts"]  = 3,
                        ["DelaySeconds"] = 1
                    },
                    ["CircuitBreaker"] = new JsonObject
                    {
                        ["FailureThreshold"]     = 5,
                        ["BreakDurationSeconds"] = 30
                    }
                };

                fileWriter.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                console.MarkupLine(string.Format(SR.Current.MongoDbAppSettingsInjected, fileName));
            }
            catch (Exception ex)
            {
                console.MarkupLine(string.Format(SR.Current.MongoDbAppSettingsWarning, fileName, ex.Message));
            }
        }
    }

    private void InjectProgramCs(string ns, string presentationPath) =>
        ExtensionHelpers.InjectProgramCs(
            presentationPath, fileWriter, console,
            new ProgramCsMessages(
                SR.Current.MongoDbProgramCsNotFound,
                SR.Current.MongoDbProgramCsAlreadyConfigured,
                SR.Current.MongoDbProgramCsInjected,
                SR.Current.MongoDbProgramCsWarning),
            content => IsProgramCsAlreadyConfigured(content, ns),
            content => InjectAddMongoDb(ExtensionHelpers.InjectPresentationUsing(content, ns)));

    private static bool IsProgramCsAlreadyConfigured(string content, string ns) =>
        content.Contains("AddMongoDb") &&
        content.Contains($"using {ns}.Presentation.Api.Extensions;");

    private static string InjectAddMongoDb(string content)
    {
        const string call = "builder.Services.AddMongoDb(builder.Configuration);";
        if (content.Contains(call)) return content;
        return ExtensionHelpers.InsertBeforeAnchor(content, "var app = builder.Build();", $"{call}\n");
    }

    public static IEnumerable<(string Path, string Content)> GetFiles(
        string ns, string appPath, string infraMongoPath, string presentationPath)
    {
        yield return (
            Path.Combine(appPath, "Interfaces", "Context", "IMongoDbContext.cs"),
            IMongoDbContextTemplate(ns));

        yield return (
            Path.Combine(infraMongoPath, "Context", "MongoDbContext.cs"),
            MongoDbContextTemplate(ns));

        yield return (
            Path.Combine(presentationPath, "Extensions", "MongoDbExtensions.cs"),
            MongoDbExtensionsTemplate(ns));
    }

    private static string IMongoDbContextTemplate(string ns) => $$"""
        using MongoDB.Driver;

        namespace {{ns}}.Application.Interfaces.Context;

        public interface IMongoDbContext
        {
            IMongoCollection<T> GetCollection<T>(string name);
            Task<TResult> ExecuteAsync<TResult>(
                Func<IMongoDatabase, CancellationToken, ValueTask<TResult>> operation,
                CancellationToken cancellationToken = default);
        }
        """;

    private static string MongoDbContextTemplate(string ns) => $$"""
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.Resilience;
        using MongoDB.Driver;
        using Polly;
        using {{ns}}.Application.Interfaces.Context;

        namespace {{ns}}.Infra.MongoDb.Context;

        public sealed class MongoDbContext(
            IMongoClient mongoClient,
            IResiliencePipelineProvider<string> pipelineProvider,
            IConfiguration configuration) : IMongoDbContext
        {
            private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline("mongodb");
            private readonly IMongoDatabase _database = mongoClient.GetDatabase(
                configuration["MongoDb:DatabaseName"] ?? "openbase");

            public IMongoCollection<T> GetCollection<T>(string name) =>
                _database.GetCollection<T>(name);

            public async Task<TResult> ExecuteAsync<TResult>(
                Func<IMongoDatabase, CancellationToken, ValueTask<TResult>> operation,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return await _pipeline.ExecuteAsync(
                        ct => operation(_database, ct), cancellationToken);
                }
                catch { return default!; }
            }
        }
        """;

    private static string MongoDbExtensionsTemplate(string ns) => $$"""
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using MongoDB.Driver;
        using Polly;
        using Polly.CircuitBreaker;
        using Polly.Retry;
        using {{ns}}.Application.Interfaces.Context;
        using {{ns}}.Infra.MongoDb.Context;

        namespace {{ns}}.Presentation.Api.Extensions;

        public static class MongoDbExtensions
        {
            public static IServiceCollection AddMongoDb(
                this IServiceCollection services, IConfiguration configuration)
            {
                services.AddSingleton<IMongoClient>(_ =>
                    new MongoClient(configuration["MongoDb:ConnectionString"]
                        ?? throw new InvalidOperationException("MongoDb:ConnectionString not configured.")));

                services.AddResiliencePipeline("mongodb", builder =>
                {
                    var maxAttempts  = configuration.GetValue("MongoDb:Retry:MaxAttempts", 3);
                    var delaySeconds = configuration.GetValue("MongoDb:Retry:DelaySeconds", 1);
                    var failureThreshold  = configuration.GetValue("MongoDb:CircuitBreaker:FailureThreshold", 5);
                    var breakDuration     = configuration.GetValue("MongoDb:CircuitBreaker:BreakDurationSeconds", 30);

                    builder.AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = maxAttempts,
                        BackoffType      = DelayBackoffType.Exponential,
                        Delay            = TimeSpan.FromSeconds(delaySeconds)
                    });

                    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                    {
                        HandledEventsAllowedBeforeBreaking = failureThreshold,
                        BreakDuration                      = TimeSpan.FromSeconds(breakDuration)
                    });
                });

                services.AddSingleton<IMongoDbContext, MongoDbContext>();

                return services;
            }
        }
        """;
}
