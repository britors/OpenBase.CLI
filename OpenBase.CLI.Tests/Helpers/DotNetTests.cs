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
}
