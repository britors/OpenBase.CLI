using Spectre.Console;

namespace OpenBase.CLI.Helpers;

internal static class ConsoleBanner
{
    private static readonly string[] Lines =
    [
        "   [blue]╔══════════╗[/]    [deepskyblue1]╭─●[/]",
        "   [blue]╠══════════╣[/]  [deepskyblue1]●─┤ ├─●[/]",
        "   [blue]║   SQL    ║[/]    [deepskyblue1]│ │[/]",
        "   [blue]╠══════════╣[/]  [deepskyblue1]●─┤ ├─●[/]",
        "   [blue]╚══════════╝[/]    [deepskyblue1]╰─●[/]",
        "",
        "  [bold blue]OpenBase[/][bold deepskyblue1]NET[/]",
        "",
    ];

    internal static void Print(IAnsiConsole? console = null)
    {
        var target = console ?? AnsiConsole.Console;
        foreach (var line in Lines)
            target.MarkupLine(line);
    }
}
