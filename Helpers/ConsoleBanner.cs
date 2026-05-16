using Spectre.Console;

namespace OpenBase.CLI.Helpers;

internal static class ConsoleBanner
{
    private static readonly string[] Lines =
    [
        "  [white]╭──────────────────╮[/]",
        "  [white]│[/] [red]●[/] [yellow]●[/] [green]●[/][white]            │[/]",
        "  [white]├──────────────────┤[/]",
        "  [white]│                  │[/]",
        "  [white]│[/]    [bold white]> _[/][white]           │[/]",
        "  [white]│                  │[/]",
        "  [white]╰──────────────────╯[/]",
        "",
        "  [bold white]OpenBase CLI[/]",
        "  [grey]Automate. Accelerate. Build Better.[/]",
        "",
    ];

    internal static void Print(IAnsiConsole? console = null)
    {
        var target = console ?? AnsiConsole.Console;
        foreach (var line in Lines)
            target.MarkupLine(line);
    }
}
