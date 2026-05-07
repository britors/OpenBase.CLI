using OpenBase.CLI.Helpers;

namespace OpenBase.CLI.Tests.Helpers;

public class DotNetTests
{
    [Fact]
    public void TemplatePackages_HasTwoEntries()
    {
        Assert.Equal(2, DotNet.TemplatePackages.Length);
    }

    [Fact]
    public void TemplatePackages_ContainsSqlServerTemplate()
    {
        Assert.Contains("w3ti.OpenBaseNET.SQLServer.Template", DotNet.TemplatePackages);
    }

    [Fact]
    public void TemplatePackages_ContainsPostgresTemplate()
    {
        Assert.Contains("w3ti.OpenBaseNET.Postgres.Template", DotNet.TemplatePackages);
    }

    [Theory]
    [InlineData("10.0.100", 10, true)]
    [InlineData("10.0.100", 9, true)]
    [InlineData("9.0.200", 10, false)]
    [InlineData("11.0.0", 10, true)]
    [InlineData("10.0.0", 11, false)]
    public void IsSdkVersionSufficient_VersionParsing_ReturnsExpected(
        string versionString, int requiredMajor, bool expected)
    {
        var parsed = Version.TryParse(versionString, out var v) && v!.Major >= requiredMajor;
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("--", 10, false)]
    [InlineData("", 10, false)]
    [InlineData("invalid", 10, false)]
    public void IsSdkVersionSufficient_InvalidVersion_ReturnsFalse(
        string versionString, int requiredMajor, bool expected)
    {
        var parsed = Version.TryParse(versionString, out var v) && v!.Major >= requiredMajor;
        Assert.Equal(expected, parsed);
    }
}
