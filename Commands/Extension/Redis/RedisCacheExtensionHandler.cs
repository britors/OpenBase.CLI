using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Extension.Redis;

public sealed class RedisCacheExtensionHandler(
    IAnsiConsole console,
    IDotNetRunner dotNetRunner,
    IFileWriter fileWriter) : IExtensionHandler
{
    private static readonly IReadOnlyList<string> Packages =
    [
        "Microsoft.Extensions.Caching.StackExchangeRedis",
        "Microsoft.Extensions.Resilience"
    ];

    public string Name => "redis";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, _, presentationPath) = paths.Value;
        var infraCachePath = Path.Combine(solutionDir, "src", $"{ns}.Infra.Cache");

        var projectReady = ExtensionHelpers.CreateDedicatedProject(
            ns, "Infra.Cache", solutionDir, infraCachePath, appPath, presentationPath,
            Packages,
            new InfraProjectMessages(
                SR.Current.InfraCacheProjectCreated,
                SR.Current.InfraCacheProjectAlreadyExists,
                SR.Current.InfraCacheProjectFailed),
            fileWriter, dotNetRunner, console);

        if (!projectReady)
            return new ExtensionApplyResult(false, SR.Current.ExtensionPackageInstallFailed);

        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraCachePath, presentationPath), solutionDir, fileWriter, console);

        ExtensionHelpers.InjectAppSettingsSection(
            presentationPath, "Redis",
            () => new JsonObject
            {
                ["ConnectionString"] = "localhost:6379",
                ["InstanceName"]     = "openbase_",
                ["Retry"] = new JsonObject { ["MaxAttempts"] = 3, ["DelaySeconds"] = 1 },
                ["CircuitBreaker"] = new JsonObject { ["MinimumThroughput"] = 5, ["FailureRatio"] = 0.5, ["SamplingDurationSeconds"] = 60, ["BreakDurationSeconds"] = 30 }
            },
            SR.Current.RedisAppSettingsInjected,
            SR.Current.RedisAppSettingsWarning,
            fileWriter, console);

        InjectProgramCs(ns, presentationPath);

        return new ExtensionApplyResult(true);
    }

    private void InjectProgramCs(string ns, string presentationPath) =>
        ExtensionHelpers.InjectProgramCs(
            presentationPath, fileWriter, console,
            new ProgramCsMessages(
                SR.Current.RedisProgramCsNotFound,
                SR.Current.RedisProgramCsAlreadyConfigured,
                SR.Current.RedisProgramCsInjected,
                SR.Current.RedisProgramCsWarning),
            content => IsProgramCsAlreadyConfigured(content, ns),
            content => InjectAddRedisCache(ExtensionHelpers.InjectPresentationUsing(content, ns)));

    private static bool IsProgramCsAlreadyConfigured(string content, string ns) =>
        content.Contains("AddRedisCache") &&
        content.Contains($"using {ns}.Presentation.Api.Extensions;");

    private static string InjectAddRedisCache(string content)
    {
        const string call = "builder.Services.AddRedisCache(builder.Configuration);";
        if (content.Contains(call)) return content;
        return ExtensionHelpers.InsertBeforeAnchor(content, "var app = builder.Build();", $"{call}\n");
    }

    public static IEnumerable<(string Path, string Content)> GetFiles(
        string ns, string appPath, string infraCachePath, string presentationPath)
    {
        yield return (
            Path.Combine(appPath, "Interfaces", "Services", "ICacheService.cs"),
            ICacheServiceTemplate(ns));

        yield return (
            Path.Combine(infraCachePath, "Services", "RedisCacheService.cs"),
            RedisCacheServiceTemplate(ns));

        yield return (
            Path.Combine(presentationPath, "Extensions", "RedisExtensions.cs"),
            RedisExtensionsTemplate(ns));
    }

    private static string ICacheServiceTemplate(string ns) => $$"""
        namespace {{ns}}.Application.Interfaces.Services;

        public interface ICacheService
        {
            Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
            Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
            Task RemoveAsync(string key, CancellationToken cancellationToken = default);
            Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        }
        """;

    private static string RedisCacheServiceTemplate(string ns) => $$"""
        using System.Text.Json;
        using Microsoft.Extensions.Caching.Distributed;
        using Polly;
        using Polly.Registry;
        using {{ns}}.Application.Interfaces.Services;

        namespace {{ns}}.Infra.Cache.Services;

        public sealed class RedisCacheService(
            IDistributedCache cache,
            ResiliencePipelineProvider<string> pipelineProvider) : ICacheService
        {
            private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline("redis");

            public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await _pipeline.ExecuteAsync(async ct =>
                    {
                        var data = await cache.GetStringAsync(key, ct);
                        return data is null ? default : JsonSerializer.Deserialize<T>(data);
                    }, cancellationToken);
                }
                catch { return default; }
            }

            public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
            {
                try
                {
                    await _pipeline.ExecuteAsync(async ct =>
                    {
                        var options = new DistributedCacheEntryOptions();
                        if (expiry.HasValue)
                            options.AbsoluteExpirationRelativeToNow = expiry;
                        await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, ct);
                    }, cancellationToken);
                }
                catch { }
            }

            public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            {
                try
                {
                    await _pipeline.ExecuteAsync(async ct => await cache.RemoveAsync(key, ct), cancellationToken);
                }
                catch { }
            }

            public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            {
                try
                {
                    return await _pipeline.ExecuteAsync(async ct =>
                        await cache.GetStringAsync(key, ct) is not null, cancellationToken);
                }
                catch { return false; }
            }
        }
        """;

    private static string RedisExtensionsTemplate(string ns) => $$"""
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Polly;
        using Polly.CircuitBreaker;
        using Polly.Retry;
        using {{ns}}.Application.Interfaces.Services;
        using {{ns}}.Infra.Cache.Services;

        namespace {{ns}}.Presentation.Api.Extensions;

        public static class RedisExtensions
        {
            public static IServiceCollection AddRedisCache(
                this IServiceCollection services, IConfiguration configuration)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration["Redis:ConnectionString"]
                        ?? throw new InvalidOperationException("Redis:ConnectionString not configured.");
                    options.InstanceName = configuration["Redis:InstanceName"];
                });

                services.AddResiliencePipeline("redis", builder =>
                {
                    var maxAttempts       = configuration.GetValue("Redis:Retry:MaxAttempts", 3);
                    var delaySeconds      = configuration.GetValue("Redis:Retry:DelaySeconds", 1);
                    var minThroughput     = configuration.GetValue("Redis:CircuitBreaker:MinimumThroughput", 5);
                    var failureRatio      = configuration.GetValue("Redis:CircuitBreaker:FailureRatio", 0.5);
                    var samplingDuration  = configuration.GetValue("Redis:CircuitBreaker:SamplingDurationSeconds", 60);
                    var breakDuration     = configuration.GetValue("Redis:CircuitBreaker:BreakDurationSeconds", 30);

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

                services.AddSingleton<ICacheService, RedisCacheService>();

                return services;
            }
        }
        """;
}
