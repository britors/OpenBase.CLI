using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

internal static class CommandTestHelper
{
    internal static CommandContext CreateContext(string name = "test")
    {
        var remaining = new Mock<IRemainingArguments>();
        remaining.Setup(r => r.Raw).Returns(Array.Empty<string>());
        remaining.Setup(r => r.Parsed)
            .Returns(Array.Empty<string>().ToLookup(s => s, s => (string?)null));
        return new CommandContext(Array.Empty<string>(), remaining.Object, name, null);
    }

    internal static IAnsiConsole CreateConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No
        });
}
