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
    private const string CachingPackageId = "Microsoft.Extensions.Caching.StackExchangeRedis";

    public string Name => "redis";
    public IReadOnlyList<string> SupportedProviders => [];

    public ExtensionApplyResult Apply(ExtensionContext context)
    {
        var paths = ExtensionHelpers.ResolveSolutionPaths(context);
        if (paths is null)
            return new ExtensionApplyResult(false, SR.Current.ExtensionRequiresOpenBaseProject);

        var (ns, solutionDir, appPath, infraDataPath, presentationPath) = paths.Value;

        ExtensionHelpers.AddPackage(
            Path.Combine(presentationPath, $"{ns}.Presentation.Api.csproj"),
            CachingPackageId, fileWriter, dotNetRunner, console);
        ExtensionHelpers.WriteFiles(GetFiles(ns, appPath, infraDataPath, presentationPath), solutionDir, fileWriter, console);
        InjectAppSettings(presentationPath);
        InjectProgramCs(ns, presentationPath);

        return new ExtensionApplyResult(true);
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
                    ["InstanceName"] = "openbase_"
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
            SR.Current.RedisProgramCsNotFound,
            SR.Current.RedisProgramCsAlreadyConfigured,
            SR.Current.RedisProgramCsInjected,
            SR.Current.RedisProgramCsWarning,
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
        string ns, string appPath, string infraDataPath, string presentationPath)
    {
        yield return (
            Path.Combine(appPath, "Interfaces", "Services", "ICacheService.cs"),
            ICacheServiceTemplate(ns));

        yield return (
            Path.Combine(infraDataPath, "Services", "RedisCacheService.cs"),
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
        using {{ns}}.Application.Interfaces.Services;

        namespace {{ns}}.Infra.Data.Services;

        public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
        {
            public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            {
                var data = await cache.GetStringAsync(key, cancellationToken);
                return data is null ? default : JsonSerializer.Deserialize<T>(data);
            }

            public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
            {
                var options = new DistributedCacheEntryOptions();
                if (expiry.HasValue)
                    options.AbsoluteExpirationRelativeToNow = expiry;
                await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, cancellationToken);
            }

            public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
                => await cache.RemoveAsync(key, cancellationToken);

            public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
                => await cache.GetStringAsync(key, cancellationToken) is not null;
        }
        """;

    private static string RedisExtensionsTemplate(string ns) => $$"""
        using Microsoft.Extensions.Caching.StackExchangeRedis;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using {{ns}}.Application.Interfaces.Services;
        using {{ns}}.Infra.Data.Services;

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

                services.AddSingleton<ICacheService, RedisCacheService>();

                return services;
            }
        }
        """;
}
