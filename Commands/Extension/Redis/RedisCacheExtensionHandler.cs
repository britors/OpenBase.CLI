using System.Text.Json;
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
    private const string CachingPackageId  = "Microsoft.Extensions.Caching.StackExchangeRedis";
    private const string ResiliencePackageId = "Microsoft.Extensions.Resilience";

    public string Name => "redis";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, _, presentationPath) = paths.Value;
        var infraCachePath = Path.Combine(solutionDir, "src", $"{ns}.Infra.Cache");

        CreateInfraCacheProject(ns, solutionDir, infraCachePath, appPath, presentationPath);
        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraCachePath, presentationPath), solutionDir, fileWriter, console);
        InjectAppSettings(presentationPath);
        InjectProgramCs(ns, presentationPath);

        return new ExtensionApplyResult(true);
    }

    private void CreateInfraCacheProject(
        string ns, string solutionDir, string infraCachePath, string appPath, string presentationPath)
    {
        var infraCacheCsproj    = Path.Combine(infraCachePath, $"{ns}.Infra.Cache.csproj");
        var appCsproj           = Path.Combine(appPath, $"{ns}.Application.csproj");
        var presentationCsproj  = Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj");

        if (!fileWriter.FileExists(infraCacheCsproj))
        {
            var (ok, err) = dotNetRunner.Run($"new classlib -n \"{ns}.Infra.Cache\" -o \"{infraCachePath}\"");
            if (!ok)
            {
                console.MarkupLine(string.Format(SR.Current.InfraCacheProjectFailed, $"{ns}.Infra.Cache", err));
                return;
            }

            var slnFile = fileWriter.FindSolutionFile(solutionDir);
            if (slnFile is not null)
                dotNetRunner.Run($"sln \"{slnFile}\" add \"{infraCacheCsproj}\"");

            console.MarkupLine(string.Format(SR.Current.InfraCacheProjectCreated, $"{ns}.Infra.Cache"));
        }
        else
        {
            console.MarkupLine(string.Format(SR.Current.InfraCacheProjectAlreadyExists, $"{ns}.Infra.Cache"));
        }

        ExtensionHelpers.AddProjectReference(infraCacheCsproj, appCsproj, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddProjectReference(presentationCsproj, infraCacheCsproj, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddPackage(infraCacheCsproj, CachingPackageId, fileWriter, dotNetRunner, console);
        ExtensionHelpers.AddPackage(infraCacheCsproj, ResiliencePackageId, fileWriter, dotNetRunner, console);
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
                if (root is null || root.ContainsKey("Redis")) continue;

                root["Redis"] = new JsonObject
                {
                    ["ConnectionString"] = "localhost:6379",
                    ["InstanceName"]     = "openbase_",
                    ["Retry"] = new JsonObject
                    {
                        ["MaxAttempts"]  = 3,
                        ["DelaySeconds"] = 1
                    },
                    ["CircuitBreaker"] = new JsonObject
                    {
                        ["FailureThreshold"]      = 5,
                        ["BreakDurationSeconds"]  = 30
                    }
                };

                fileWriter.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                console.MarkupLine(string.Format(SR.Current.RedisAppSettingsInjected, fileName));
            }
            catch (Exception ex)
            {
                console.MarkupLine(string.Format(SR.Current.RedisAppSettingsWarning, fileName, ex.Message));
            }
        }
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
        using Microsoft.Extensions.Resilience;
        using Polly;
        using {{ns}}.Application.Interfaces.Services;

        namespace {{ns}}.Infra.Cache.Services;

        public sealed class RedisCacheService(
            IDistributedCache cache,
            IResiliencePipelineProvider<string> pipelineProvider) : ICacheService
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
                    await _pipeline.ExecuteAsync(ct => cache.RemoveAsync(key, ct), cancellationToken);
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
                    var maxAttempts  = configuration.GetValue("Redis:Retry:MaxAttempts", 3);
                    var delaySeconds = configuration.GetValue("Redis:Retry:DelaySeconds", 1);
                    var failureThreshold  = configuration.GetValue("Redis:CircuitBreaker:FailureThreshold", 5);
                    var breakDuration     = configuration.GetValue("Redis:CircuitBreaker:BreakDurationSeconds", 30);

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

                services.AddSingleton<ICacheService, RedisCacheService>();

                return services;
            }
        }
        """;
}
