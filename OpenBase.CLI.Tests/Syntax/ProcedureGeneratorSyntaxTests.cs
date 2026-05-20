using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OpenBase.CLI.Commands.Procedure;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Syntax;

public class ProcedureGeneratorSyntaxTests
{
    private static ProcedureContext MakeContext(
        string name = "GetOrderById",
        string ns   = "MyApp",
        string dir  = "/sol",
        IReadOnlyList<ProcedureParameter>? parameters = null) =>
        new(name, ns, dir)
        {
            Parameters = parameters ??
            [
                new ProcedureParameter("OrderId",    "int",     ParameterDirection.In),
                new ProcedureParameter("TotalAmount","decimal", ParameterDirection.Out)
            ]
        };

    [Fact]
    public void GetFiles_AllGeneratedFiles_HaveValidCSharpSyntax()
    {
        var files = new ProcedureGenerator(MakeContext()).GetFiles().ToList();

        foreach (var (path, content) in files)
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_OnlyInputParams_AllFilesHaveValidSyntax()
    {
        var ctx = MakeContext(parameters:
        [
            new ProcedureParameter("OrderId",   "int",    ParameterDirection.In),
            new ProcedureParameter("CustomerId","int",    ParameterDirection.In),
        ]);

        foreach (var (path, content) in new ProcedureGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_OnlyOutputParams_AllFilesHaveValidSyntax()
    {
        var ctx = MakeContext(parameters:
        [
            new ProcedureParameter("Total",  "decimal", ParameterDirection.Out),
            new ProcedureParameter("Status", "string",  ParameterDirection.Out),
        ]);

        foreach (var (path, content) in new ProcedureGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_NoParams_AllFilesHaveValidSyntax()
    {
        var ctx = MakeContext(parameters: []);

        foreach (var (path, content) in new ProcedureGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    [Fact]
    public void GetFiles_InOutParams_AllFilesHaveValidSyntax()
    {
        var ctx = MakeContext(parameters:
        [
            new ProcedureParameter("Value", "int", ParameterDirection.InOut),
        ]);

        foreach (var (path, content) in new ProcedureGenerator(ctx).GetFiles())
            AssertValidSyntax(path, content);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("short")]
    [InlineData("string")]
    [InlineData("bool")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("float")]
    [InlineData("DateTime")]
    [InlineData("DateTimeOffset")]
    [InlineData("DateOnly")]
    [InlineData("TimeOnly")]
    [InlineData("Guid")]
    public void GetFiles_EachOutputCsType_ProducesValidSyntax(string csType)
    {
        var ctx = MakeContext(parameters:
        [
            new ProcedureParameter("InputId", "int",   ParameterDirection.In),
            new ProcedureParameter("Result",  csType,  ParameterDirection.Out),
        ]);

        foreach (var (path, content) in new ProcedureGenerator(ctx).GetFiles())
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
