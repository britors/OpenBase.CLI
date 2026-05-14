using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Helpers;

public class ExtensionRegistryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly ExtensionRegistry _registry = new();

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void GetAll_NoRegistryFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_tempDir);

        var result = _registry.GetAll(_tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public void IsInstalled_NotRegistered_ReturnsFalse()
    {
        Directory.CreateDirectory(_tempDir);

        Assert.False(_registry.IsInstalled(_tempDir, "jwt", null));
    }

    [Fact]
    public void Register_And_GetAll_ReturnsSavedEntry()
    {
        Directory.CreateDirectory(_tempDir);
        var entry = new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow);

        _registry.Register(_tempDir, entry);
        var result = _registry.GetAll(_tempDir);

        Assert.Single(result);
        Assert.Equal("jwt", result[0].Name);
    }

    [Fact]
    public void IsInstalled_AfterRegister_ReturnsTrue()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow));

        Assert.True(_registry.IsInstalled(_tempDir, "jwt", null));
    }

    [Fact]
    public void IsInstalled_CaseInsensitive_ReturnsTrue()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow));

        Assert.True(_registry.IsInstalled(_tempDir, "JWT", null));
    }

    [Fact]
    public void IsInstalled_WithProvider_MatchesProvider()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("cache", "redis", DateTimeOffset.UtcNow));

        Assert.True(_registry.IsInstalled(_tempDir, "cache", "redis"));
        Assert.False(_registry.IsInstalled(_tempDir, "cache", "memory"));
    }

    [Fact]
    public void IsInstalled_WithNullProvider_IgnoresProvider()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("cache", "redis", DateTimeOffset.UtcNow));

        Assert.True(_registry.IsInstalled(_tempDir, "cache", null));
    }

    [Fact]
    public void Register_MultipleExtensions_AllPersisted()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow));
        _registry.Register(_tempDir, new ExtensionEntry("cache", "redis", DateTimeOffset.UtcNow));

        var result = _registry.GetAll(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Register_CreatesOpenbaseDirectory()
    {
        Directory.CreateDirectory(_tempDir);
        _registry.Register(_tempDir, new ExtensionEntry("jwt", null, DateTimeOffset.UtcNow));

        Assert.True(Directory.Exists(Path.Combine(_tempDir, ".openbase")));
        Assert.True(File.Exists(Path.Combine(_tempDir, ".openbase", "extensions.json")));
    }
}
