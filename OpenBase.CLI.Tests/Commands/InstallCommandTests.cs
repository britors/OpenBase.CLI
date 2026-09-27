using System.Text.Json;
using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Creation;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Models;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public sealed class InstallCommandTests
{
    [Theory]
    [InlineData(false, "11.0.0")]
    [InlineData(true, "11.1.0-preview.10")]
    public void ResolvesOnlyCompatibleMajorAndExplicitPreviews(bool preview, string expected)
        => Assert.Equal(expected, PackageCatalog.Select(["10.9.0", "11.0.0", "11.1.0-preview.2", "11.1.0-preview.10", "12.0.0"], PackageIds.Unified, null, preview));
    [Theory]
    [InlineData("12.0.0")]
    [InlineData("10.0.0")]
    public void CannotPinIncompatibleVersion(string version)
        => Assert.Throws<CliException>(() => PackageCatalog.Select([version], PackageIds.Unified, version, true));
    [Theory]
    [InlineData("*")]
    [InlineData("11.*")]
    [InlineData("11.0.0 --source evil")]
    [InlineData("11.0.0-preview.01")]
    public void RejectsInvalidVersions(string version)
        => Assert.Equal("VERSION_INVALID", Assert.Throws<CliException>(() => PackageCatalog.Select([], PackageIds.Unified, version, false)).Code);
    [Fact]
    public void ExplicitPreviewPinDoesNotRequireFlag()
        => Assert.Equal("11.0.0-preview.1", PackageCatalog.Select(["11.0.0-preview.1"], PackageIds.Unified, "11.0.0-preview.1", false));
    [Theory]
    [InlineData("template", PackageIds.Unified, "11.0.0")]
    [InlineData("postgres", PackageIds.Postgres, "2.0.0")]
    [InlineData("sqlserver", PackageIds.SqlServer, "2.0.0")]
    [InlineData("oracle", PackageIds.Oracle, "2.0.0")]
    [InlineData("cli", PackageIds.Cli, "10.14.0")]
    public async Task ExplicitComponentsInstallOnlyRequestedPackage(string type, string package, string version)
    {
        var runner = new Mock<IDotNetRunner>(); var catalog = new Mock<IPackageCatalog>(); var history = new Mock<IUpdateHistoryService>();
        runner.Setup(r => r.IsSdkVersionSufficient(10)).Returns(true);
        runner.Setup(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), null)).ReturnsAsync((true, ""));
        runner.Setup(r => r.GetInstalledTemplateVersionAsync(package, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        runner.Setup(r => r.GetInstalledToolVersionAsync(package, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        catalog.Setup(c => c.VersionsAsync(package, It.IsAny<CancellationToken>())).ReturnsAsync([version]);
        var command = new InstallCommand(new(runner.Object, catalog.Object, history.Object, CommandTestHelper.CreateConsole()));
        Assert.Equal(0, await ((ICommand<InstallSettings>)command).ExecuteAsync(CommandTestHelper.CreateContext(), new() { Type = type }, default));
        runner.Verify(r => r.RunAsync(It.Is<IReadOnlyList<string>>(a => type == "cli" ? a.Contains(package) : a.Contains(package + "::" + version)), It.IsAny<CancellationToken>(), null), Times.Once);
        history.Verify(h => h.AddEntryAsync(It.Is<UpdateHistoryEntry>(e => e.Component == package && e.Success), It.IsAny<CancellationToken>()), Times.Once);
    }
}
