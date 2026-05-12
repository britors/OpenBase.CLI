using OpenBase.CLI.Commands;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Tests.Commands;

public class NewSettingsTests
{
    private static NewSettings Valid(string name = "MeuProjeto", string template = "sqlserver") =>
        new() { Name = name, TemplateName = template, Type = "api" };

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var settings = Valid(name: "");
        var result = settings.Validate();
        Assert.False(result.Successful);
        Assert.Contains("--name", result.Message);
    }

    [Fact]
    public void Validate_WhitespaceName_ReturnsError()
    {
        var settings = Valid(name: "   ");
        var result = settings.Validate();
        Assert.False(result.Successful);
        Assert.Contains("--name", result.Message);
    }

    [Theory]
    [InlineData("Meu Projeto")]
    [InlineData("proj&eto")]
    [InlineData("proj|eto")]
    [InlineData("proj;eto")]
    [InlineData("proj`eto")]
    [InlineData("proj$eto")]
    [InlineData("proj(eto")]
    [InlineData("proj)eto")]
    public void Validate_InvalidCharsInName_ReturnsError(string name)
    {
        var settings = Valid(name: name);
        var result = settings.Validate();
        Assert.False(result.Successful);
        Assert.Contains("caracteres inválidos", result.Message);
    }

    [Theory]
    [InlineData("MeuProjeto")]
    [InlineData("meu-projeto")]
    [InlineData("meu_projeto")]
    [InlineData("Projeto123")]
    public void Validate_ValidName_ReturnsSuccess(string name)
    {
        var settings = Valid(name: name);
        var result = settings.Validate();
        Assert.True(result.Successful);
    }
}
