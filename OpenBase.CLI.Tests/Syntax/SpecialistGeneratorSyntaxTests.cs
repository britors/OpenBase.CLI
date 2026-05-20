using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OpenBase.CLI.Commands.Scaffold;

namespace OpenBase.CLI.Tests.Syntax;

public class SpecialistGeneratorSyntaxTests
{
    private static ScaffoldContext MakeContext(string entity = "Produto", string ns = "MyApp") =>
        new(entity, ns, "/sol")
        {
            Properties = [new Models.EntityProperty("Name", "string", true)]
        };

    private static SpecialistDefinition QueryDef(string method = "FindByCategory", bool paged = false) =>
        new(method, SpecialistType.Query,
            "SELECT Name, Price FROM Products WHERE CategoryId = {{ CategoryId }}",
            [new SpecialistParam("CategoryId", "int")])
        {
            ResultColumns = [new SpecialistParam("Name", "string"), new SpecialistParam("Price", "decimal")],
            IsPaginated   = paged
        };

    private static SpecialistDefinition CommandDef(string method = "Deactivate") =>
        new(method, SpecialistType.Command,
            "UPDATE Products SET Active = 0 WHERE Id = {{ Id }}",
            [new SpecialistParam("Id", "int")]);

    [Fact]
    public void QuerySpecialist_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(QueryDef()))
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void QuerySpecialist_Paginated_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(QueryDef(paged: true)))
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void QuerySpecialist_NoParams_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var def = new SpecialistDefinition("GetAll", SpecialistType.Query,
            "SELECT Name FROM Products", [])
        {
            ResultColumns = [new SpecialistParam("Name", "string")]
        };

        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(def))
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void CommandSpecialist_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(CommandDef()))
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void CommandSpecialist_NoParams_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var def = new SpecialistDefinition("ArchiveAll", SpecialistType.Command,
            "UPDATE Products SET Archived = 1", []);

        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(def))
            AssertValidSyntax(path, content);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("Guid")]
    [InlineData("decimal")]
    [InlineData("DateTime")]
    [InlineData("bool")]
    public void QuerySpecialist_EachParamType_ProducesValidSyntax(string csType)
    {
        var def = new SpecialistDefinition("FindByValue", SpecialistType.Query,
            "SELECT Name FROM Products WHERE Value = {{ Value }}",
            [new SpecialistParam("Value", csType)])
        {
            ResultColumns = [new SpecialistParam("Name", "string")]
        };

        var generator = new ScaffoldGenerator(MakeContext());
        foreach (var (path, content) in generator.GetSpecialistFiles(def))
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
