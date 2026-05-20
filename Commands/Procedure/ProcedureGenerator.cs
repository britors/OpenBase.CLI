using System.Text;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Procedure;

public sealed class ProcedureGenerator(ProcedureContext ctx)
{
    private const string I4  = "    ";
    private const string I8  = "        ";
    private const string I12 = "            ";

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
            if (p.CsType is "int" or "long" or "short")
                rules.Add($"RuleFor(x => x.{p.Name}).NotEmpty();");
            else if (p.CsType == "Guid")
                rules.Add($"RuleFor(x => x.{p.Name}).NotEmpty();");
            else if (p.CsType == "string")
                rules.Add($"RuleFor(x => x.{p.Name})\n{I12}.NotEmpty()\n{I12}.MaximumLength(255);");
        }

        return rules.Count == 0 ? string.Empty : string.Join($"\n\n{I8}", rules);
    }

    private string TestValue(ProcedureParameter p) => p.CsType switch
    {
        "int" or "long" or "short" => "1",
        "string"                   => "\"Test\"",
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

    private string DefaultOutputTestValue(ProcedureParameter p) => p.CsType switch
    {
        "int" or "long" or "short" => "0",
        "string"                   => "string.Empty",
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

        foreach (var p in ctx.InputParams.Where(p => p.CsType is "int" or "long" or "short" or "Guid"))
        {
            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}IsZero()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.Validate(new Execute{ctx.ProcedureName}Command({TestArgsOverride(p, "0")}));");
            sb.Append($"\n{I8}Assert.False(result.IsValid);");
            sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"{p.Name}\");");
            sb.Append($"\n{I4}}}");
        }

        foreach (var p in ctx.InputParams.Where(p => p.CsType == "string"))
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

    private string RepositoryImplementationTemplate() => $$"""
        using {{ctx.NS}}.Application.DTOs.{{ctx.ProcedureName}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.ProcedureName}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed class {{ctx.ProcedureName}}Repository : I{{ctx.ProcedureName}}Repository
        {
            public Task<{{ctx.ProcedureName}}Response> ExecuteAsync(Execute{{ctx.ProcedureName}}Command command, CancellationToken cancellationToken)
            {
                // TODO: implement stored procedure/package call
                throw new NotImplementedException();
            }
        }
        """;

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
