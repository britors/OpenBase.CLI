using OpenBase.CLI.Commands;
using OpenBase.CLI.Localization;

namespace OpenBase.CLI.Tests.Commands;

public class ScaffoldSettingsTests
{
    private static ScaffoldSettings Valid(string entity = "Produto") => new() { Entity = entity };

    [Fact]
    public void Validate_EmptyEntity_ReturnsError()
    {
        var result = Valid(entity: "").Validate();
        Assert.False(result.Successful);
        Assert.Contains("--entity", result.Message);
    }

    [Fact]
    public void Validate_WhitespaceEntity_ReturnsError()
    {
        var result = Valid(entity: "   ").Validate();
        Assert.False(result.Successful);
        Assert.Contains("--entity", result.Message);
    }

    [Theory]
    [InlineData("produto")]
    [InlineData("minhEntidade")]
    public void Validate_LowercaseStart_ReturnsError(string entity)
    {
        var result = Valid(entity: entity).Validate();
        Assert.False(result.Successful);
        Assert.Contains("PascalCase", result.Message);
    }

    [Theory]
    [InlineData("Minha Entidade")]
    [InlineData("Minha-Entidade")]
    [InlineData("Minha_Entidade")]
    [InlineData("Produto!")]
    public void Validate_SpecialCharsInEntity_ReturnsError(string entity)
    {
        var result = Valid(entity: entity).Validate();
        Assert.False(result.Successful);
        Assert.Equal(SR.Current.EntityMustBeAlphanumeric, result.Message);
    }

    [Theory]
    [InlineData("Produto")]
    [InlineData("MinhaEntidade")]
    [InlineData("Produto123")]
    [InlineData("A")]
    public void Validate_ValidEntity_ReturnsSuccess(string entity)
    {
        var result = Valid(entity: entity).Validate();
        Assert.True(result.Successful);
    }
}
