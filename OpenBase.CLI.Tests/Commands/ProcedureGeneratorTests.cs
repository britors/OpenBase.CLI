using OpenBase.CLI.Commands.Procedure;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Tests.Commands;

public class ProcedureGeneratorTests
{
    private static ProcedureContext MakeContext(
        string name = "GetOrderById",
        string ns   = "MyApp",
        string dir  = "/sol",
        IReadOnlyList<ProcedureParameter>? parameters = null) =>
        new(name, ns, dir)
        {
            Parameters = parameters ?? [
                new ProcedureParameter("OrderId",   "int",    ParameterDirection.In),
                new ProcedureParameter("TotalAmount","decimal",ParameterDirection.Out)
            ]
        };

    private static ProcedureGenerator MakeGenerator(
        string name = "GetOrderById",
        IReadOnlyList<ProcedureParameter>? parameters = null) =>
        new(MakeContext(name, parameters: parameters));

    [Fact]
    public void GetFiles_Returns8Files()
    {
        var files = MakeGenerator().GetFiles().ToList();
        Assert.Equal(8, files.Count);
    }

    [Fact]
    public void GetFiles_AllPathsAreUnique()
    {
        var paths = MakeGenerator().GetFiles().Select(f => f.Path).ToList();
        Assert.Equal(paths.Count, paths.Distinct().Count());
    }

    [Fact]
    public void GetFiles_AllContentsAreNonEmpty()
    {
        var files = MakeGenerator().GetFiles().ToList();
        Assert.All(files, f => Assert.False(string.IsNullOrWhiteSpace(f.Content)));
    }

    [Fact]
    public void GetFiles_ProcedureNameAppearsInAllContents()
    {
        var files = MakeGenerator("GetOrderById").GetFiles().ToList();
        Assert.All(files, f => Assert.Contains("GetOrderById", f.Content));
    }

    [Fact]
    public void GetFiles_NamespaceAppearsInAllContents()
    {
        var ctx   = MakeContext(ns: "MinhaEmpresa");
        var files = new ProcedureGenerator(ctx).GetFiles().ToList();
        Assert.All(files, f => Assert.Contains("MinhaEmpresa", f.Content));
    }

    [Fact]
    public void GetFiles_CommandContainsInputParams()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var commandFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommand.cs"));
        Assert.Contains("OrderId", commandFile.Content);
        Assert.Contains("int",     commandFile.Content);
    }

    [Fact]
    public void GetFiles_ResponseContainsOutputParams()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var responseFile = files.Single(f => f.Path.EndsWith("GetOrderByIdResponse.cs"));
        Assert.Contains("TotalAmount", responseFile.Content);
        Assert.Contains("decimal",     responseFile.Content);
    }

    [Fact]
    public void GetFiles_NoParams_CommandHasEmptyParams()
    {
        var files = MakeGenerator(parameters: []).GetFiles().ToList();
        var commandFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommand.cs"));
        Assert.Contains("Execute", commandFile.Content);
    }

    [Fact]
    public void GetFiles_NoOutputParams_ResponseHasBoolSuccess()
    {
        var files = MakeGenerator(parameters: [new ProcedureParameter("OrderId", "int", ParameterDirection.In)]).GetFiles().ToList();
        var responseFile = files.Single(f => f.Path.EndsWith("GetOrderByIdResponse.cs"));
        Assert.Contains("bool Success", responseFile.Content);
    }

    [Fact]
    public void GetFiles_HandlerUsesRepositoryInterface()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var handlerFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommandHandler.cs"));
        Assert.Contains("IGetOrderByIdRepository", handlerFile.Content);
    }

    [Fact]
    public void GetFiles_ValidatorContainsRuleForIntInput()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var validatorFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommandValidator.cs"));
        Assert.Contains("RuleFor", validatorFile.Content);
        Assert.Contains("OrderId", validatorFile.Content);
    }

    [Fact]
    public void GetFiles_ValidatorIsEmpty_WhenNoInputParams()
    {
        var files = MakeGenerator(parameters: [new ProcedureParameter("Result", "decimal", ParameterDirection.Out)]).GetFiles().ToList();
        var validatorFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommandValidator.cs"));
        Assert.DoesNotContain("RuleFor", validatorFile.Content);
    }

    [Fact]
    public void GetFiles_RepositoryInterfaceHasExecuteAsync()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var ifFile = files.Single(f => Path.GetFileName(f.Path) == "IGetOrderByIdRepository.cs");
        Assert.Contains("ExecuteAsync", ifFile.Content);
    }

    [Fact]
    public void GetFiles_RepositoryImplementationThrowsNotImplemented()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var implFile = files.Single(f => Path.GetFileName(f.Path) == "GetOrderByIdRepository.cs");
        Assert.Contains("NotImplementedException", implFile.Content);
    }

    [Fact]
    public void GetFiles_HandlerTestContainsCallsRepositoryFact()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var testFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommandHandlerTests.cs"));
        Assert.Contains("Handle_ValidCommand_CallsRepository", testFile.Content);
    }

    [Fact]
    public void GetFiles_ValidatorTestContainsIsValidFact()
    {
        var files = MakeGenerator().GetFiles().ToList();
        var testFile = files.Single(f => f.Path.EndsWith("ExecuteGetOrderByIdCommandValidatorTests.cs"));
        Assert.Contains("Validate_IsValid_WhenAllPropertiesAreProvided", testFile.Content);
    }

    [Fact]
    public void Context_PCamel_IsLowercaseFirstLetter()
    {
        var ctx = new ProcedureContext("GetOrderById", "NS", "/");
        Assert.Equal("getOrderById", ctx.PCamel);
    }

    [Fact]
    public void Context_InputParams_FiltersCorrectly()
    {
        var ctx = MakeContext(parameters: [
            new ProcedureParameter("A", "int",    ParameterDirection.In),
            new ProcedureParameter("B", "string", ParameterDirection.Out),
            new ProcedureParameter("C", "bool",   ParameterDirection.InOut)
        ]);
        Assert.Equal(2, ctx.InputParams.Count);
        Assert.Equal(2, ctx.OutputParams.Count);
    }

    [Fact]
    public void Context_OutputParams_FiltersCorrectly()
    {
        var ctx = MakeContext(parameters: [
            new ProcedureParameter("A", "int",    ParameterDirection.In),
            new ProcedureParameter("B", "string", ParameterDirection.Out),
        ]);
        Assert.Single(ctx.OutputParams);
        Assert.Equal("B", ctx.OutputParams[0].Name);
    }
}
