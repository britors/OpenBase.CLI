using OpenBase.CLI.Helpers.Execution;

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

    [Theory]
    [InlineData("w3ti.openbase.cli   10.5.11   openbase", "w3ti.openbase.cli", "10.5.11")]
    [InlineData("W3TI.OPENBASE.CLI   10.5.11   openbase", "w3ti.openbase.cli", "10.5.11")]
    [InlineData("other.tool          1.0.0      other\nw3ti.openbase.cli   2.0.0   openbase", "w3ti.openbase.cli", "2.0.0")]
    public void ParseToolVersion_ValidOutput_ReturnsVersion(string output, string packageId, string expected)
    {
        Assert.Equal(expected, DotNet.ParseToolVersion(output, packageId));
    }

    [Theory]
    [InlineData("", "w3ti.openbase.cli")]
    [InlineData("other.tool   1.0.0   other", "w3ti.openbase.cli")]
    public void ParseToolVersion_PackageNotFound_ReturnsNull(string output, string packageId)
    {
        Assert.Null(DotNet.ParseToolVersion(output, packageId));
    }

    [Fact]
    public void ParseTemplateVersion_ValidOutput_ReturnsVersion()
    {
        var output = """
            Currently installed items:
               w3ti.OpenBaseNET.SQLServer.Template
                 Version: 2.1.0
                 Author: Rodrigo Brito
            """;

        Assert.Equal("2.1.0", DotNet.ParseTemplateVersion(output, "w3ti.OpenBaseNET.SQLServer.Template"));
    }

    [Fact]
    public void ParseTemplateVersion_CaseInsensitivePackageId_ReturnsVersion()
    {
        var output = """
            Currently installed items:
               w3ti.OpenBaseNET.SQLServer.Template
                 Version: 3.0.0
            """;

        Assert.Equal("3.0.0", DotNet.ParseTemplateVersion(output, "W3TI.OPENBASENET.SQLSERVER.TEMPLATE"));
    }

    [Fact]
    public void ParseTemplateVersion_MultiplePackages_ReturnsCorrectVersion()
    {
        var output = """
            Currently installed items:
               w3ti.OpenBaseNET.SQLServer.Template
                 Version: 2.0.0
               w3ti.OpenBaseNET.Postgres.Template
                 Version: 1.5.3
            """;

        Assert.Equal("1.5.3", DotNet.ParseTemplateVersion(output, "w3ti.OpenBaseNET.Postgres.Template"));
    }

    [Fact]
    public void ParseTemplateVersion_PortugueseOutput_ReturnsVersion()
    {
        var output = """
            Itens instalados no momento:
               w3ti.OpenBaseNET.SQLServer.Template
                  Versão: 10.3.1
                  Detalhes:
                     Author: w3ti
               w3ti.OpenBaseNET.Postgres.Template
                  Versão: 10.3.0
            """;

        Assert.Equal("10.3.1", DotNet.ParseTemplateVersion(output, "w3ti.OpenBaseNET.SQLServer.Template"));
        Assert.Equal("10.3.0", DotNet.ParseTemplateVersion(output, "w3ti.OpenBaseNET.Postgres.Template"));
    }

    [Theory]
    [InlineData("", "w3ti.OpenBaseNET.SQLServer.Template")]
    [InlineData("Currently installed items:\n   other.package\n     Version: 1.0.0", "w3ti.OpenBaseNET.SQLServer.Template")]
    public void ParseTemplateVersion_PackageNotFound_ReturnsNull(string output, string packageId)
    {
        Assert.Null(DotNet.ParseTemplateVersion(output, packageId));
    }
}
