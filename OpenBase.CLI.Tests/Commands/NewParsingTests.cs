using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Creation;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class NewParsingTests
{
    [Theory]
    [InlineData("-d")]
    [InlineData("--database")]
    [InlineData("-s")]
    [InlineData("--template")]
    public async Task MissingArrayOptionValueIsAnArgumentError(string option)
    {
        var app = new CommandApp();
        string? error = null;
        app.Configure(config =>
        {
            config.UseStrictParsing();
            config.AddDelegate<NewSettings>("new", (_, settings, _) =>
            {
                error = Assert.Throws<CliException>(() => settings.SelectDatabase()).Code;
                return 2;
            });
        });
        Assert.Equal(2, await app.RunAsync(["new", "-n", "Test", option, "--json"]));
        Assert.Equal("ARGUMENT_INVALID", error);
    }
}
