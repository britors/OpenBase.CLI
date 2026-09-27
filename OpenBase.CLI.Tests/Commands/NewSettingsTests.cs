using OpenBase.CLI.Commands;
using OpenBase.CLI.Helpers.Creation;

namespace OpenBase.CLI.Tests.Commands;

public class NewSettingsTests
{
    [Theory]
    [InlineData("postgres", "postgres")]
    [InlineData("POSTGRES", "postgres")]
    [InlineData("pgsql", "postgres")]
    [InlineData("postgresql", "postgres")]
    [InlineData("sqlserver", "sqlserver")]
    [InlineData("oracle", "oracle")]
    public void SelectsCanonicalDatabase(string input, string expected)
        => Assert.Equal(expected, new NewSettings { Name = "Acme.Customers", Databases = [input] }.SelectDatabase());

    [Theory]
    [InlineData("", "INPUT_REQUIRED")]
    [InlineData("A-B", "NAME_INVALID")]
    [InlineData("1Api", "NAME_INVALID")]
    [InlineData("Acme.class", "NAME_INVALID")]
    [InlineData("@class", "NAME_INVALID")]
    [InlineData("Api\n", "NAME_INVALID")]
    [InlineData("A;touch bad", "NAME_INVALID")]
    public void RejectsInvalidNames(string name, string error)
        => Assert.Equal(error, Assert.Throws<CliException>(() => new NewSettings { Name = name }.SelectDatabase()).Code);

    [Fact]
    public void RepeatedAliasesAgree()
        => Assert.Equal("postgres", new NewSettings { Name = "A", Databases = ["PGSQL", "postgres"], Templates = ["postgresql"] }.SelectDatabase());

    [Theory]
    [InlineData("sql")]
    [InlineData("mssql")]
    [InlineData("0")]
    [InlineData("sqlite")]
    public void RejectsUnsupportedDatabases(string input)
        => Assert.Equal("DATABASE_UNSUPPORTED", Assert.Throws<CliException>(() => new NewSettings { Name = "A", Databases = [input] }.SelectDatabase()).Code);

    [Fact]
    public void RejectsConflictingRepeatedOptions()
        => Assert.Equal("ARGUMENT_CONFLICT", Assert.Throws<CliException>(() => new NewSettings { Name = "A", Databases = ["oracle", "postgres"] }.SelectDatabase()).Code);

    [Fact]
    public void RejectsConflictingLegacyOption()
        => Assert.Equal("ARGUMENT_CONFLICT", Assert.Throws<CliException>(() => new NewSettings { Name = "A", Databases = ["oracle"], Templates = ["pgsql"] }.SelectDatabase()).Code);
}
