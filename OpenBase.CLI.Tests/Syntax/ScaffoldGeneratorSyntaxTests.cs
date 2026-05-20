using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Syntax;

public class ScaffoldGeneratorSyntaxTests
{
    private static ScaffoldContext MakeContext(
        string entity = "Produto",
        string ns     = "MyApp",
        string dir    = "/sol") =>
        new(entity, ns, dir)
        {
            Properties = [new EntityProperty("Name", "string", true), new EntityProperty("Price", "decimal", true)]
        };

    [Fact]
    public void GetFiles_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var generator = new ScaffoldGenerator(MakeContext());
        var files = generator.GetFiles().ToList();

        foreach (var (path, content) in files)
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_NoProperties_AllFilesHaveValidSyntax()
    {
        var ctx = new ScaffoldContext("Produto", "MyApp", "/sol") { Properties = [] };
        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();

        foreach (var (path, content) in files)
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_MultipleProperties_AllFilesHaveValidSyntax()
    {
        var ctx = new ScaffoldContext("Pedido", "MyApp", "/sol")
        {
            Properties =
            [
                new EntityProperty("Name",      "string",   true),
                new EntityProperty("Price",     "decimal",  true),
                new EntityProperty("Quantity",  "int",      false),
                new EntityProperty("Active",    "bool",     true),
                new EntityProperty("CreatedAt", "DateTime", false),
            ]
        };

        foreach (var (path, content) in new ScaffoldGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    [Theory]
    [InlineData("SqlServer", DbFlavor.SqlServer)]
    [InlineData("Postgres",  DbFlavor.Postgres)]
    [InlineData("Oracle",    DbFlavor.Oracle)]
    public void GetFiles_AllDbFlavors_AllFilesHaveValidSyntax(string _, DbFlavor flavor)
    {
        var ctx = new ScaffoldContext("Produto", "MyApp", "/sol")
        {
            DbFlavor   = flavor,
            Properties = [new EntityProperty("Name", "string", true)]
        };

        foreach (var (path, content) in new ScaffoldGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    private static void AssertValidSyntax(string path, string content)
    {
        var tree   = CSharpSyntaxTree.ParseText(content);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(errors.Count == 0,
            $"Syntax errors in {Path.GetFileName(path)}:\n" +
            string.Join("\n", errors.Select(e => e.ToString())));
    }
}
