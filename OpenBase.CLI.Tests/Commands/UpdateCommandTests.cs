using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;
public class UpdateCommandTests
{
    [Theory]
    [InlineData("11.0.0", "11.1.0", true, 0)]
    [InlineData("11.1.0", "11.0.0", true, 3)]
    [InlineData("11.0.0", "11.1.0", false, 4)]
    public async Task UpdateTracksResultAndRefusesDowngrade(string installed, string available, bool succeeds, int expected)
    {
        var runner = new Mock<IDotNetRunner>(); var catalog = new Mock<IPackageCatalog>(); var history = new Mock<IUpdateHistoryService>();
        runner.Setup(r => r.IsSdkVersionSufficient(10)).Returns(true);
        runner.SetupSequence(r => r.GetInstalledTemplateVersionAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync(installed).ReturnsAsync(available);
        runner.Setup(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), null)).ReturnsAsync((succeeds, ""));
        catalog.Setup(c => c.VersionsAsync(PackageIds.Unified, It.IsAny<CancellationToken>())).ReturnsAsync([available]);
        var command = new UpdateCommand(new(runner.Object, catalog.Object, history.Object, CommandTestHelper.CreateConsole()));
        Assert.Equal(expected, await ((ICommand<UpdateSettings>)command).ExecuteAsync(CommandTestHelper.CreateContext(), new(), default));
        runner.Verify(r => r.GetInstalledToolVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        if (expected == 3) runner.Verify(r => r.RunAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), null), Times.Never);
    }
}
