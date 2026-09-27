using System.Text.Json;
using System.Text.Json.Nodes;
using OpenBase.CLI.Helpers.Creation;

namespace OpenBase.CLI.Tests.Helpers;

public sealed class GeneratedManifestTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "openbase-manifest-" + Guid.NewGuid());
    private readonly JsonObject manifest;
    public GeneratedManifestTests()
    {
        Directory.CreateDirectory(root);
        var projects = new JsonObject();
        foreach (var role in new[] { "Domain", "Application", "Infrastructure", "Api" })
        {
            var path = $"src/Any {role}/Any.{role}.csproj";
            projects[role.ToLowerInvariant()] = path;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, path))!);
            File.WriteAllText(Path.Combine(root, path), role == "Infrastructure"
                ? "<Project><ItemGroup><PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" /></ItemGroup></Project>" : "<Project />");
        }
        File.WriteAllText(Path.Combine(root, "Any.sln"), string.Join('\n', projects.Select(p => $"Project(\"GUID\") = \"Any\", \"{p.Value!.GetValue<string>()}\", \"GUID\"")));
        manifest = new JsonObject
        {
            ["schemaVersion"] = 2, ["template"] = "openbasenet", ["templateVersion"] = "11.0.0-preview.1", ["architecture"] = "hexagonal",
            ["database"] = "postgres", ["rootNamespace"] = "Any", ["solution"] = "Any.sln", ["projects"] = projects,
            ["persistence"] = new JsonObject { ["dbContext"] = "Any.Infrastructure.OpenBaseDbContext", ["migrationsPath"] = "src/Any Infrastructure/Persistence/Migrations", ["connectionStringName"] = "Default" }
        };
    }
    private GeneratedManifest Read()
    {
        File.WriteAllText(Path.Combine(root, ".openbase.json"), manifest.ToJsonString());
        return GeneratedManifest.Read(root, "postgres", "Any", "11.0.0-preview.1");
    }
    [Fact]
    public void ResolvesRenamedApiWithSpacesAndMissingMigrationsDirectory()
        => Assert.Equal(Path.Combine(root, "src/Any Api/Any.Api.csproj"), Read().ApiProject);
    [Theory]
    [InlineData("../outside.csproj")]
    [InlineData("/absolute.csproj")]
    [InlineData("C:/drive.csproj")]
    [InlineData("src\\Any.csproj")]
    [InlineData("src/./Any.csproj")]
    [InlineData("src//Any.csproj")]
    public void RejectsUnsafePaths(string path)
    {
        manifest["projects"]!["api"] = path;
        Assert.Equal("MANIFEST_INVALID", Assert.Throws<CliException>(Read).Code);
    }
    [Fact]
    public void RejectsUnknownProperties()
    {
        manifest["password"] = "never-valid";
        Assert.Throws<CliException>(Read);
    }
    [Fact]
    public void DistinguishesUnsupportedSchema()
    {
        manifest["schemaVersion"] = 3;
        Assert.Equal("MANIFEST_VERSION_UNSUPPORTED", Assert.Throws<CliException>(Read).Code);
    }
    [Fact]
    public void RejectsProviderMismatch()
    {
        manifest["database"] = "oracle";
        Assert.Throws<CliException>(Read);
    }
    [Fact]
    public void RejectsConditionalProvider()
    {
        File.WriteAllText(Path.Combine(root, "src/Any Infrastructure/Any.Infrastructure.csproj"), "<Project><ItemGroup Condition=\"false\"><PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" /></ItemGroup></Project>");
        Assert.Throws<CliException>(Read);
    }
    [Fact]
    public void RejectsProjectAbsentFromSolution()
    {
        File.WriteAllText(Path.Combine(root, "Any.sln"), "");
        Assert.Throws<CliException>(Read);
    }
    [Fact]
    public void RejectsPathThroughSymlink()
    {
        if (OperatingSystem.IsWindows()) return; // symlink creation requires elevated privileges on Windows
        var outside = Path.Combine(Path.GetTempPath(), "openbase-outside-" + Guid.NewGuid());
        Directory.CreateDirectory(outside);
        try
        {
            File.WriteAllText(Path.Combine(outside, "api.csproj"), "<Project />");
            Directory.CreateSymbolicLink(Path.Combine(root, "linked"), outside);
            manifest["projects"]!["api"] = "linked/api.csproj";
            Assert.Throws<CliException>(Read);
        }
        finally { Directory.Delete(outside, true); }
    }
    public void Dispose() => Directory.Delete(root, true);
}
