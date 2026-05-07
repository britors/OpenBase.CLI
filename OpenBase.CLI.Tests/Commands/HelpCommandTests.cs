using OpenBase.CLI.Commands;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class HelpCommandTests
{
    [Fact]
    public async Task Execute_ReturnsZero()
    {
        var command = new HelpCommand();
        var context = CommandTestHelper.CreateContext("help");

        var result = await ((ICommand<HelpSettings>)command)
            .ExecuteAsync(context, new HelpSettings(), CancellationToken.None);

        Assert.Equal(0, result);
    }
}
