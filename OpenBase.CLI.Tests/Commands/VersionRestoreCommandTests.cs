using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;
public class VersionRestoreCommandTests
{
    [Fact]
    public async Task RestoreAllowsExplicitDowngrade()
    {
        var runner = new Mock<IDotNetRunner>(); var catalog = new Mock<IPackageCatalog>(); var history = new Mock<IUpdateHistoryService>();
        runner.Setup(r => r.IsSdkVersionSufficient(10)).Returns(true);
        runner.SetupSequence(r => r.GetInstalledTemplateVersionAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync("11.1.0").ReturnsAsync("11.0.0");
        runner.Setup(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), null)).ReturnsAsync((true, ""));
        catalog.Setup(c => c.VersionsAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync(["11.0.0"]);
        var command = new VersionRestoreCommand(new(runner.Object, catalog.Object, history.Object, CommandTestHelper.CreateConsole()));
        Assert.Equal(0, await ((ICommand<VersionRestoreSettings>)command).ExecuteAsync(CommandTestHelper.CreateContext(), new() { Version = "11.0.0" }, default));
    }
}
