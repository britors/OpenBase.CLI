using System.Text;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Procedure;

public sealed class ProcedureGenerator(ProcedureContext ctx)
{
    private const string I4      = "    ";
    private const string I8      = "        ";
    private const string I12     = "            ";
    private const string CsShort  = "short";
    private const string CsString = "string";

    public IEnumerable<(string Path, string Content)> GetFiles()
    {
        var feat     = Path.Combine(ctx.AppPath, "Features", $"{ctx.ProcedureName}Feature");
        var dtoRes   = Path.Combine(ctx.AppPath, "DTOs", ctx.ProcedureName, "Responses");
        var domainIf = Path.Combine(ctx.DomainPath, "Interfaces", "Repositories");
        var infraRep = Path.Combine(ctx.InfraDataPath, "Repositories");
        var testFeat = Path.Combine(ctx.TestsPath, "Features", $"{ctx.ProcedureName}FeatureTests");

        yield return (Path.Combine(feat,    $"Execute{ctx.ProcedureName}Command.cs"),           CommandTemplate());
        yield return (Path.Combine(feat,    $"Execute{ctx.ProcedureName}CommandHandler.cs"),    CommandHandlerTemplate());
        yield return (Path.Combine(feat,    $"Execute{ctx.ProcedureName}CommandValidator.cs"),  CommandValidatorTemplate());
        yield return (Path.Combine(dtoRes,  $"{ctx.ProcedureName}Response.cs"),                 ResponseTemplate());
        yield return (Path.Combine(domainIf,$"I{ctx.ProcedureName}Repository.cs"),              RepositoryInterfaceTemplate());
        yield return (Path.Combine(infraRep,$"{ctx.ProcedureName}Repository.cs"),               RepositoryImplementationTemplate());
        yield return (Path.Combine(testFeat,$"Execute{ctx.ProcedureName}CommandHandlerTests.cs"),   HandlerTestTemplate());
        yield return (Path.Combine(testFeat,$"Execute{ctx.ProcedureName}CommandValidatorTests.cs"), ValidatorTestTemplate());
    }


    private string InputParamsDecl() =>
        ctx.InputParams.Count == 0
            ? string.Empty
            : string.Join(", ", ctx.InputParams.Select(p => $"{p.CsType} {p.Name}"));

    private string OutputParamsDecl() =>
        ctx.OutputParams.Count == 0
            ? "bool Success"
            : string.Join(", ", ctx.OutputParams.Select(p => $"{p.CsType} {p.Name}"));

    private string ValidatorRules()
    {
        var rules = new List<string>();

        foreach (var p in ctx.InputParams)
        {
            if (p.CsType is "int" or "long" or CsShort)
                rules.Add($"RuleFor(x => x.{p.Name}).NotEmpty();");
            else if (p.CsType == "Guid")
                rules.Add($"RuleFor(x => x.{p.Name}).NotEmpty();");
            else if (p.CsType == CsString)
                rules.Add($"RuleFor(x => x.{p.Name})\n{I12}.NotEmpty()\n{I12}.MaximumLength(255);");
        }

        return rules.Count == 0 ? string.Empty : string.Join($"\n\n{I8}", rules);
    }

    private static string TestValue(ProcedureParameter p) => p.CsType switch
    {
        "int" or "long" or CsShort => "1",
        CsString                   => "\"Test\"",
        "bool"                     => "true",
        "decimal"                  => "1.0m",
        "double"                   => "1.0",
        "float"                    => "1.0f",
        "Guid"                     => "Guid.NewGuid()",
        "DateTime"                 => "DateTime.Now",
        "DateTimeOffset"           => "DateTimeOffset.Now",
        "DateOnly"                 => "DateOnly.FromDateTime(DateTime.Now)",
        "TimeOnly"                 => "TimeOnly.FromDateTime(DateTime.Now)",
        _                          => "default"
    };

    private static string DefaultOutputTestValue(ProcedureParameter p) => p.CsType switch
    {
        "int" or "long" or CsShort => "0",
        CsString                   => "string.Empty",
        "bool"                     => "false",
        "decimal"                  => "0m",
        "double"                   => "0.0",
        "float"                    => "0.0f",
        "Guid"                     => "Guid.Empty",
        "DateTime"                 => "DateTime.MinValue",
        "DateTimeOffset"           => "DateTimeOffset.MinValue",
        "DateOnly"                 => "DateOnly.MinValue",
        "TimeOnly"                 => "TimeOnly.MinValue",
        _                          => "default"
    };

    private string InputTestArgs() =>
        ctx.InputParams.Count == 0 ? string.Empty
            : string.Join(", ", ctx.InputParams.Select(TestValue));

    private string OutputTestArgs() =>
        ctx.OutputParams.Count == 0 ? "false"
            : string.Join(", ", ctx.OutputParams.Select(DefaultOutputTestValue));

    private string TestArgsOverride(ProcedureParameter target, string overrideValue) =>
        ctx.InputParams.Count == 0 ? string.Empty
            : string.Join(", ", ctx.InputParams.Select(p => p == target ? overrideValue : TestValue(p)));

    private string BuildValidatorTestMethods()
    {
        var sb = new StringBuilder();

        foreach (var p in ctx.InputParams.Where(p => p.CsType is "int" or "long" or CsShort or "Guid"))
        {
            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}IsZero()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.Validate(new Execute{ctx.ProcedureName}Command({TestArgsOverride(p, "0")}));");
            sb.Append($"\n{I8}Assert.False(result.IsValid);");
            sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"{p.Name}\");");
            sb.Append($"\n{I4}}}");
        }

        foreach (var p in ctx.InputParams.Where(p => p.CsType == CsString))
        {
            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}IsEmpty()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.Validate(new Execute{ctx.ProcedureName}Command({TestArgsOverride(p, "\"\"")}));");
            sb.Append($"\n{I8}Assert.False(result.IsValid);");
            sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"{p.Name}\");");
            sb.Append($"\n{I4}}}");
        }

        return sb.ToString();
    }


    private string CommandTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;

        public sealed record Execute{{ctx.ProcedureName}}Command({{InputParamsDecl()}}) : IRequest<{{ctx.ProcedureName}}Response>;
        """;

    private string ResponseTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;

        public readonly record struct {{ctx.ProcedureName}}Response({{OutputParamsDecl()}});
        """;

    private string CommandHandlerTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;

        public sealed class Execute{{ctx.ProcedureName}}CommandHandler(I{{ctx.ProcedureName}}Repository repository)
            : IRequestHandler<Execute{{ctx.ProcedureName}}Command, {{ctx.ProcedureName}}Response>
        {
            public async Task<{{ctx.ProcedureName}}Response>
                Handle(Execute{{ctx.ProcedureName}}Command request, CancellationToken cancellationToken)
            {
                return await repository.ExecuteAsync(request, cancellationToken);
            }
        }
        """;

    private string CommandValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;

        public sealed class Execute{{ctx.ProcedureName}}CommandValidator : AbstractValidator<Execute{{ctx.ProcedureName}}Command>
        {
            public Execute{{ctx.ProcedureName}}CommandValidator()
            {
                {{ValidatorRules()}}
            }
        }
        """;

    private string RepositoryInterfaceTemplate() => $$"""
        using {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;

        namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

        public interface I{{ctx.ProcedureName}}Repository
        {
            Task<{{ctx.ProcedureName}}Response> ExecuteAsync(Execute{{ctx.ProcedureName}}Command command, CancellationToken cancellationToken);
        }
        """;

    private string RepositoryImplementationTemplate()
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Dapper;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine($"using {ctx.NS}.Application.DTOs.{ctx.ProcedureName}.Responses;");
        sb.AppendLine($"using {ctx.NS}.Application.Features.{ctx.ProcedureName}Feature;");
        sb.AppendLine($"using {ctx.NS}.Domain.Interfaces.Repositories;");
        sb.AppendLine($"using {ctx.NS}.Infra.Data.Context;");
        sb.AppendLine($"using {ctx.NS}.Infra.Dapper.Extension;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ctx.NS}.Infra.Data.Repositories;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class {ctx.ProcedureName}Repository(DbSession dbSession) : I{ctx.ProcedureName}Repository");
        sb.AppendLine("{");
        sb.AppendLine($"{I4}public async Task<{ctx.ProcedureName}Response> ExecuteAsync(Execute{ctx.ProcedureName}Command command, CancellationToken cancellationToken)");
        sb.AppendLine($"{I4}{{");

        var hasParams = ctx.Parameters.Count > 0;

        if (hasParams)
        {
            sb.AppendLine($"{I8}var parameters = new DynamicParameters();");
            foreach (var p in ctx.InputParams)
                sb.AppendLine($"{I8}parameters.Add(\"@{p.Name}\", command.{p.Name});");
            foreach (var p in ctx.OutputParams)
                sb.AppendLine($"{I8}parameters.Add(\"@{p.Name}\", dbType: DbType.{CsTypeToDbType(p.CsType)}, direction: ParameterDirection.Output);");
            sb.AppendLine();
        }

        sb.AppendLine($"{I8}await dbSession.Connection!.ExecuteAsyncWithRetry(");
        sb.AppendLine($"{I12}cancellationToken,");
        sb.AppendLine($"{I12}sql: \"{ctx.ProcedureName}\",");
        sb.AppendLine($"{I12}transaction: dbSession.Transaction,");
        if (hasParams)
        {
            sb.AppendLine($"{I12}commandType: CommandType.StoredProcedure,");
            sb.AppendLine($"{I12}parameters: parameters);");
        }
        else
        {
            sb.AppendLine($"{I12}commandType: CommandType.StoredProcedure);");
        }

        sb.AppendLine();

        if (ctx.OutputParams.Count == 0)
        {
            sb.AppendLine($"{I8}return new {ctx.ProcedureName}Response(true);");
        }
        else if (ctx.OutputParams.Count == 1)
        {
            var p = ctx.OutputParams[0];
            sb.AppendLine($"{I8}return new {ctx.ProcedureName}Response({p.Name}: parameters.Get<{p.CsType}>(\"@{p.Name}\"));");
        }
        else
        {
            sb.AppendLine($"{I8}return new {ctx.ProcedureName}Response(");
            for (var i = 0; i < ctx.OutputParams.Count; i++)
            {
                var p   = ctx.OutputParams[i];
                var end = i < ctx.OutputParams.Count - 1 ? "," : ");";
                sb.AppendLine($"{I12}{p.Name}: parameters.Get<{p.CsType}>(\"@{p.Name}\"){end}");
            }
        }

        sb.AppendLine($"{I4}}}");
        sb.Append("}");

        return sb.ToString();
    }

    private static string CsTypeToDbType(string csType) => csType switch
    {
        "int"            => "Int32",
        "long"           => "Int64",
        CsShort          => "Int16",
        "bool"           => "Boolean",
        "decimal"        => "Decimal",
        "double"         => "Double",
        "float"          => "Single",
        "DateTime"       => "DateTime",
        "DateTimeOffset" => "DateTimeOffset",
        "DateOnly"       => "Date",
        "TimeOnly"       => "Time",
        "Guid"           => "Guid",
        "byte[]"         => "Binary",
        _                => "String"
    };

    private string HandlerTestTemplate() => $$"""
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Tests.Unit.Features.{{ctx.ProcedureName}}FeatureTests;

        public class Execute{{ctx.ProcedureName}}CommandHandlerTests
        {
            private readonly Mock<I{{ctx.ProcedureName}}Repository> _repository = new();
            private readonly Execute{{ctx.ProcedureName}}CommandHandler _handler;

            public Execute{{ctx.ProcedureName}}CommandHandlerTests()
            {
                _handler = new Execute{{ctx.ProcedureName}}CommandHandler(_repository.Object);
            }

            [Fact]
            public async Task Handle_ValidCommand_CallsRepository()
            {
                var command = new Execute{{ctx.ProcedureName}}Command({{InputTestArgs()}});
                _repository
                    .Setup(r => r.ExecuteAsync(command, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new {{ctx.ProcedureName}}Response({{OutputTestArgs()}}));

                await _handler.Handle(command, CancellationToken.None);

                _repository.Verify(r => r.ExecuteAsync(command, CancellationToken.None), Times.Once);
            }

            [Fact]
            public async Task Handle_ValidCommand_ReturnsRepositoryResult()
            {
                var command = new Execute{{ctx.ProcedureName}}Command({{InputTestArgs()}});
                var expected = new {{ctx.ProcedureName}}Response({{OutputTestArgs()}});
                _repository
                    .Setup(r => r.ExecuteAsync(command, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expected);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.Equal(expected, result);
            }
        }
        """;

    private string ValidatorTestTemplate() => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Features.{{ctx.ProcedureName}}FeatureTests;

        public class Execute{{ctx.ProcedureName}}CommandValidatorTests
        {
            private readonly Execute{{ctx.ProcedureName}}CommandValidator _validator = new();

            [Fact]
            public void Validate_IsValid_WhenAllPropertiesAreProvided()
            {
                var result = _validator.Validate(new Execute{{ctx.ProcedureName}}Command({{InputTestArgs()}}));
                Assert.True(result.IsValid);
            }
        {{BuildValidatorTestMethods()}}
        }
        """;
}
