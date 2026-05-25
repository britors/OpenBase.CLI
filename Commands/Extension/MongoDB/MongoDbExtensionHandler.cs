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
    private static readonly IReadOnlyList<string> Packages =
    [
        "MongoDB.Driver",
        "Microsoft.Extensions.Resilience"
    ];

    public string Name => "mongodb";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, _, presentationPath) = paths.Value;
        var infraMongoPath = Path.Combine(solutionDir, "src", $"{ns}.Infra.MongoDb");

        var projectReady = ExtensionHelpers.CreateDedicatedProject(
            ns, "Infra.MongoDb", solutionDir, infraMongoPath, appPath, presentationPath,
            Packages,
            new InfraProjectMessages(
                SR.Current.InfraMongoDbProjectCreated,
                SR.Current.InfraMongoDbProjectAlreadyExists,
                SR.Current.InfraMongoDbProjectFailed),
            fileWriter, dotNetRunner, console);

        if (!projectReady)
            return new ExtensionApplyResult(false, SR.Current.ExtensionPackageInstallFailed);

        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraMongoPath, presentationPath), solutionDir, fileWriter, console);

        ExtensionHelpers.InjectAppSettingsSection(
            presentationPath, "MongoDb",
            () => new JsonObject
            {
                ["ConnectionString"] = "mongodb://localhost:27017",
                ["DatabaseName"]     = "openbase",
                ["Retry"] = new JsonObject { ["MaxAttempts"] = 3, ["DelaySeconds"] = 1 },
                ["CircuitBreaker"] = new JsonObject { ["MinimumThroughput"] = 5, ["FailureRatio"] = 0.5, ["SamplingDurationSeconds"] = 60, ["BreakDurationSeconds"] = 30 }
            },
            SR.Current.MongoDbAppSettingsInjected,
            SR.Current.MongoDbAppSettingsWarning,
            fileWriter, console);

        InjectProgramCs(ns, presentationPath);

        return new ExtensionApplyResult(true);
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
        namespace {{ns}}.Application.Interfaces.Context;

        public interface IMongoDbContext
        {
            Task<TResult> ExecuteAsync<TResult>(
                Func<CancellationToken, ValueTask<TResult>> operation,
                CancellationToken cancellationToken = default);
        }
        """;

    private static string MongoDbContextTemplate(string ns) => $$"""
        using Microsoft.Extensions.Configuration;
        using MongoDB.Driver;
        using Polly;
        using Polly.Registry;
        using {{ns}}.Application.Interfaces.Context;

        namespace {{ns}}.Infra.MongoDb.Context;

        public sealed class MongoDbContext(
            IMongoClient mongoClient,
            ResiliencePipelineProvider<string> pipelineProvider,
            IConfiguration configuration) : IMongoDbContext
        {
            private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline("mongodb");
            private readonly IMongoDatabase _database = mongoClient.GetDatabase(
                configuration["MongoDb:DatabaseName"] ?? "openbase");

            public IMongoCollection<T> GetCollection<T>(string name) =>
                _database.GetCollection<T>(name);

            public async Task<TResult> ExecuteAsync<TResult>(
                Func<CancellationToken, ValueTask<TResult>> operation,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return await _pipeline.ExecuteAsync(operation, cancellationToken);
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
                    var maxAttempts      = configuration.GetValue("MongoDb:Retry:MaxAttempts", 3);
                    var delaySeconds     = configuration.GetValue("MongoDb:Retry:DelaySeconds", 1);
                    var minThroughput    = configuration.GetValue("MongoDb:CircuitBreaker:MinimumThroughput", 5);
                    var failureRatio     = configuration.GetValue("MongoDb:CircuitBreaker:FailureRatio", 0.5);
                    var samplingDuration = configuration.GetValue("MongoDb:CircuitBreaker:SamplingDurationSeconds", 60);
                    var breakDuration    = configuration.GetValue("MongoDb:CircuitBreaker:BreakDurationSeconds", 30);

                    builder.AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = maxAttempts,
                        BackoffType      = DelayBackoffType.Exponential,
                        Delay            = TimeSpan.FromSeconds(delaySeconds)
                    });

                    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                    {
                        MinimumThroughput = minThroughput,
                        FailureRatio      = failureRatio,
                        SamplingDuration  = TimeSpan.FromSeconds(samplingDuration),
                        BreakDuration     = TimeSpan.FromSeconds(breakDuration)
                    });
                });

                services.AddSingleton<IMongoDbContext, MongoDbContext>();

                return services;
            }
        }
        """;
}
