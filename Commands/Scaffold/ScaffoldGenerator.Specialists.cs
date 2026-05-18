using System.Text;

namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private const string CsString = "string";

    public IEnumerable<(string Path, string Content)> GetSpecialistFiles(SpecialistDefinition def) =>
        def.Type switch
        {
            SpecialistType.Query    => QuerySpecialistFiles(def),
            SpecialistType.Command  => CommandSpecialistFiles(def),
            SpecialistType.HttpCall => HttpCallSpecialistFiles(def.MethodName),
            _                      => []
        };

    // ─── shared param helpers ──────────────────────────────────────────────────

    private static string MethodParams(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(x => $"{x.CsType} {x.PascalName}"));

    private static string MethodParamsLeading(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0 ? string.Empty : MethodParams(p) + ", ";

    private static string CallArgsLeading(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0 ? string.Empty : string.Join(", ", p.Select(x => x.PascalName)) + ", ";

    private static string CallArgs(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(x => x.PascalName));

    private static string RequestArgs(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(x => $"request.{x.PascalName}"));

    private static string ToKebab(string s) =>
        string.Concat(s.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? $"-{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));

    private static string RecordParams(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(x => $"{x.CsType} {x.PascalName}"));

    private static string RequestCallArgs(IReadOnlyList<SpecialistParam> p) =>
        (p.Count == 0 ? string.Empty : string.Join(", ", p.Select(x => $"request.{x.PascalName}")) + ", ");

    private static string DapperAnon(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0
            ? "new { }"
            : $"new {{ {string.Join(", ", p.Select(x => $"{x.PascalName} = {x.PascalName}"))} }}";

    // ─── Query ─────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> QuerySpecialistFiles(SpecialistDefinition def)
    {
        var method  = def.MethodName;
        var p       = def.Parameters;
        var cols    = def.ResultColumns;
        var paged   = def.IsPaginated;
        var feat    = Path.Combine(ctx.AppPath, Features, $"{ctx.Entity}Features", $"{method}Feature");
        var sql     = SpecialistParam.ToParameterizedSql(def.Sql, p);

        yield return (
            Path.Combine(ctx.DomainPath, "QueryResults", $"{method}QueryResult.cs"),
            QueryResultRecordTemplate(method, cols));
        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Repositories, $"I{ctx.Entity}Repository.{method}.cs"),
            QueryRepositoryInterfacePartial(method, p, paged));
        yield return (
            Path.Combine(ctx.InfraDataPath, Repositories, $"{ctx.Entity}Repository.{method}.cs"),
            QueryRepositoryPartial(method, p, sql, paged));
        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Services, $"I{ctx.Entity}DomainService.{method}.cs"),
            QueryInterfacePartial(method, p, paged));
        yield return (
            Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.{method}.cs"),
            QueryServicePartial(method, p, paged));
        yield return (Path.Combine(feat, $"{method}Query.cs"),          QuerySpecTemplate(method, p, paged));
        yield return (Path.Combine(feat, $"{method}QueryHandler.cs"),   QueryHandlerSpecTemplate(method, p, paged));
        yield return (Path.Combine(feat, $"{method}QueryValidator.cs"), QueryValidatorSpecTemplate(method));
        yield return (
            Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Requests, $"{method}Request.cs"),
            SpecialistRequestDtoTemplate(method, p));
        yield return (
            Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Responses, $"{method}Response.cs"),
            QueryResponseDtoTemplate(method, cols));
        yield return (
            Path.Combine(ctx.AppPath, "Mappers", $"{method}MapperProfile.cs"),
            QueryMapperProfileTemplate(method, paged));
        yield return (
            Path.Combine(ctx.AppPath, Interfaces, Services, $"I{ctx.Entity}ApplicationService.{method}.cs"),
            QueryIAppServicePartial(method, paged));
        yield return (
            Path.Combine(ctx.AppPath, Services, $"{ctx.Entity}ApplicationService.{method}.cs"),
            QueryAppServicePartial(method, p, paged));
        yield return (
            Path.Combine(ctx.PresentationPath, "Controllers", $"{ctx.Entity}Controller.{method}.cs"),
            QueryControllerPartial(method));

        var featTests   = Path.Combine(ctx.TestsPath, Application, Features, $"{ctx.Entity}Features");
        var appSvcTests = Path.Combine(ctx.TestsPath, Application, Services);
        yield return (Path.Combine(featTests, $"{method}QueryHandlerTests.cs"),          QueryHandlerTestsTemplate(method, p, cols, paged));
        yield return (Path.Combine(featTests, $"{method}QueryValidatorTests.cs"),         QueryValidatorTestsTemplate(method, p));
        yield return (Path.Combine(appSvcTests, $"{ctx.Entity}{method}AppServiceTests.cs"), QueryAppServiceTestsTemplate(method, p));
    }

    private string QueryRepositoryInterfacePartial(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var returnType = paged
            ? $"PaginatedQueryResult<{method}QueryResult>"
            : $"IReadOnlyList<{method}QueryResult>";
        return $$"""
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

            public partial interface I{{ctx.Entity}}Repository
            {
                Task<{{returnType}}> {{method}}Async(
                    {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
            }
            """;
    }

    private string QueryRepositoryPartial(string method, IReadOnlyList<SpecialistParam> p, string sql, bool paged)
    {
        var returnType = paged
            ? $"PaginatedQueryResult<{method}QueryResult>"
            : $"IReadOnlyList<{method}QueryResult>";
        var returnExpr = paged
            ? $"new PaginatedQueryResult<{method}QueryResult>(0, 0, 0, [..result])"
            : "[..result]";
        return $$"""
            using Dapper;
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Infra.Data.Repositories;

            public sealed partial class {{ctx.Entity}}Repository
            {
                private const string {{method}}Sql = "{{sql.Replace("\"", "\\\"")}}";

                public async Task<{{returnType}}> {{method}}Async(
                    {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
                {
                    var result = await dbSession.Connection!.QueryAsync<{{method}}QueryResult>(
                        {{method}}Sql, {{DapperAnon(p)}});
                    return {{returnExpr}};
                }
            }
            """;
    }

    private string QueryInterfacePartial(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var returnType = paged
            ? $"PaginatedQueryResult<{method}QueryResult>"
            : $"IReadOnlyList<{method}QueryResult>";
        return $$"""
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Domain.Interfaces.Services;

            public partial interface I{{ctx.Entity}}DomainService
            {
                Task<{{returnType}}> {{method}}Async(
                    {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
            }
            """;
    }

    private string QueryServicePartial(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var returnType = paged
            ? $"PaginatedQueryResult<{method}QueryResult>"
            : $"IReadOnlyList<{method}QueryResult>";
        return $$"""
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Domain.Services;

            public sealed partial class {{ctx.Entity}}DomainService
            {
                public async Task<{{returnType}}> {{method}}Async(
                    {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
                    => await {{ctx.ECamel}}Repository.{{method}}Async(
                        {{CallArgsLeading(p)}}cancellationToken);
            }
            """;
    }

    private string QuerySpecTemplate(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var responseType = paged
            ? $"PaginatedResponse<{method}Response>"
            : $"IReadOnlyList<{method}Response>";
        var paginatedUsing = paged
            ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n"
            : string.Empty;
        return $$"""
            using MediatR;
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

            namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

            public sealed record {{method}}Query({{RecordParams(p)}})
                : IRequest<{{responseType}}>;
            """;
    }

    private string QueryHandlerSpecTemplate(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var responseType   = paged ? $"PaginatedResponse<{method}Response>" : $"IReadOnlyList<{method}Response>";
        var paginatedUsing = paged ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n" : string.Empty;
        return $$"""
            using AutoMapper;
            using MediatR;
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
            using {{ctx.NS}}.Domain.Interfaces.Services;

            namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

            internal sealed class {{method}}QueryHandler(
                    I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                    IMapper mapper)
                : IRequestHandler<{{method}}Query, {{responseType}}>
            {
                public async Task<{{responseType}}>
                    Handle({{method}}Query request, CancellationToken cancellationToken)
                {
                    var result = await {{ctx.ECamel}}DomainService.{{method}}Async(
                        {{RequestCallArgs(p)}}cancellationToken);
                    return mapper.Map<{{responseType}}>(result);
                }
            }
            """;
    }

    private string QueryValidatorSpecTemplate(string method) => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed class {{method}}QueryValidator : AbstractValidator<{{method}}Query>
        {
            public {{method}}QueryValidator() { }
        }
        """;

    // ─── Command ───────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> CommandSpecialistFiles(SpecialistDefinition def)
    {
        var method = def.MethodName;
        var feat   = Path.Combine(ctx.AppPath, Features, $"{ctx.Entity}Features", $"{method}Feature");
        var sql    = SpecialistParam.ToParameterizedSql(def.Sql, def.Parameters);

        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Repositories, $"I{ctx.Entity}Repository.{method}.cs"),
            CommandRepositoryInterfacePartial(method, def.Parameters));
        yield return (
            Path.Combine(ctx.InfraDataPath, Repositories, $"{ctx.Entity}Repository.{method}.cs"),
            CommandRepositoryPartial(method, def.Parameters, sql));
        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Services, $"I{ctx.Entity}DomainService.{method}.cs"),
            CommandInterfacePartial(method, def.Parameters));
        yield return (
            Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.{method}.cs"),
            CommandServicePartial(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}Command.cs"),          CommandSpecTemplate(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}CommandHandler.cs"),   CommandHandlerSpecTemplate(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}CommandValidator.cs"), CommandValidatorSpecTemplate(method, def.Parameters));
        yield return (
            Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Requests, $"{method}Request.cs"),
            SpecialistRequestDtoTemplate(method, def.Parameters));
        yield return (
            Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Responses, $"{method}Response.cs"),
            CommandResponseDtoTemplate(method));
        yield return (
            Path.Combine(ctx.AppPath, Interfaces, Services, $"I{ctx.Entity}ApplicationService.{method}.cs"),
            CommandIAppServicePartial(method));
        yield return (
            Path.Combine(ctx.AppPath, Services, $"{ctx.Entity}ApplicationService.{method}.cs"),
            CommandAppServicePartial(method, def.Parameters));
        yield return (
            Path.Combine(ctx.PresentationPath, "Controllers", $"{ctx.Entity}Controller.{method}.cs"),
            CommandControllerPartial(method));

        var featTests   = Path.Combine(ctx.TestsPath, Application, Features, $"{ctx.Entity}Features");
        var appSvcTests = Path.Combine(ctx.TestsPath, Application, Services);
        yield return (Path.Combine(featTests, $"{method}CommandHandlerTests.cs"),           CommandHandlerTestsTemplate(method, def.Parameters));
        yield return (Path.Combine(featTests, $"{method}CommandValidatorTests.cs"),          CommandValidatorTestsTemplate(method, def.Parameters));
        yield return (Path.Combine(appSvcTests, $"{ctx.Entity}{method}AppServiceTests.cs"), CommandAppServiceTestsTemplate(method, def.Parameters));
    }

    private string CommandRepositoryInterfacePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

        public partial interface I{{ctx.Entity}}Repository
        {
            Task<bool> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
        }
        """;

    private string CommandRepositoryPartial(string method, IReadOnlyList<SpecialistParam> p, string sql) => $$"""
        using Dapper;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed partial class {{ctx.Entity}}Repository
        {
            private const string {{method}}Sql = "{{sql.Replace("\"", "\\\"")}}";

            public async Task<bool> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
            {
                var affected = await dbSession.Connection!.ExecuteAsync(
                    {{method}}Sql, {{DapperAnon(p)}});
                return affected > 0;
            }
        }
        """;

    private string CommandInterfacePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public partial interface I{{ctx.Entity}}DomainService
        {
            Task<bool> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
        }
        """;

    private string CommandServicePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        namespace {{ctx.NS}}.Domain.Services;

        public sealed partial class {{ctx.Entity}}DomainService
        {
            public async Task<bool> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
                => await {{ctx.ECamel}}Repository.{{method}}Async(
                    {{CallArgsLeading(p)}}cancellationToken);
        }
        """;

    private string CommandSpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Command({{RecordParams(p)}}) : IRequest<{{method}}Response>;
        """;

    private string CommandHandlerSpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        internal sealed class {{method}}CommandHandler(I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService)
            : IRequestHandler<{{method}}Command, {{method}}Response>
        {
            public async Task<{{method}}Response> Handle({{method}}Command request, CancellationToken cancellationToken)
            {
                var success = await {{ctx.ECamel}}DomainService.{{method}}Async(
                    {{RequestCallArgs(p)}}cancellationToken);
                return new {{method}}Response(success);
            }
        }
        """;

    private string CommandValidatorSpecTemplate(string method, IReadOnlyList<SpecialistParam> p)
    {
        var rules = p
            .Where(x => x.CsType is CsString or "Guid")
            .Select(x => $"RuleFor(x => x.{x.PascalName}).NotEmpty();")
            .ToList();

        var body = rules.Count == 0 ? string.Empty : string.Join($"\n{I8}", rules);

        return $$"""
            using FluentValidation;

            namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

            public sealed class {{method}}CommandValidator : AbstractValidator<{{method}}Command>
            {
                public {{method}}CommandValidator()
                {
                    {{body}}
                }
            }
            """;
    }

    // ─── Query — Application + Presentation ───────────────────────────────────

    private string QueryIAppServicePartial(string method, bool paged)
    {
        var responseType   = paged ? $"PaginatedResponse<{method}Response>" : $"IReadOnlyList<{method}Response>";
        var paginatedUsing = paged ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n" : string.Empty;
        return $$"""
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
            using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

            namespace {{ctx.NS}}.Application.Interfaces.Services;

            public partial interface I{{ctx.Entity}}ApplicationService
            {
                Task<{{responseType}}> {{method}}Async(
                    {{method}}Request request, CancellationToken cancellationToken = default);
            }
            """;
    }

    private string QueryAppServicePartial(string method, IReadOnlyList<SpecialistParam> p, bool paged)
    {
        var responseType   = paged ? $"PaginatedResponse<{method}Response>" : $"IReadOnlyList<{method}Response>";
        var paginatedUsing = paged ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n" : string.Empty;
        return $$"""
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
            using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
            using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

            namespace {{ctx.NS}}.Application.Services;

            public sealed partial class {{ctx.Entity}}ApplicationService
            {
                public async Task<{{responseType}}> {{method}}Async(
                    {{method}}Request request, CancellationToken cancellationToken = default)
                {
                    var query = new {{method}}Query({{RequestArgs(p)}});
                    return await mediator.Send(query, cancellationToken);
                }
            }
            """;
    }

    private string QueryControllerPartial(string method) => $$"""
        using Microsoft.AspNetCore.Mvc;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        namespace {{ctx.NS}}.Presentation.Api.Controllers;

        public partial class {{ctx.Entity}}Controller
        {
            /// <summary>{{method}}.</summary>
            [HttpGet("{{ToKebab(method)}}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            public async Task<IActionResult> {{method}}Async(
                [FromQuery] {{method}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.{{method}}Async(
                    request, cancellationToken);
                return Ok(result);
            }
        }
        """;

    // ─── Command — Application + Presentation ─────────────────────────────────

    private string CommandIAppServicePartial(string method) => $$"""
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Interfaces.Services;

        public partial interface I{{ctx.Entity}}ApplicationService
        {
            Task<{{method}}Response> {{method}}Async(
                {{method}}Request request, CancellationToken cancellationToken = default);
        }
        """;

    private string CommandAppServicePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        namespace {{ctx.NS}}.Application.Services;

        public sealed partial class {{ctx.Entity}}ApplicationService
        {
            public async Task<{{method}}Response> {{method}}Async(
                {{method}}Request request, CancellationToken cancellationToken = default)
            {
                var command = new {{method}}Command({{RequestArgs(p)}});
                return await mediator.Send(command, cancellationToken);
            }
        }
        """;

    private string CommandControllerPartial(string method) => $$"""
        using Microsoft.AspNetCore.Mvc;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        namespace {{ctx.NS}}.Presentation.Api.Controllers;

        public partial class {{ctx.Entity}}Controller
        {
            /// <summary>{{method}}.</summary>
            [HttpPost("{{ToKebab(method)}}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            public async Task<IActionResult> {{method}}Async(
                [FromBody] {{method}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.{{method}}Async(
                    request, cancellationToken);
                return Ok(result);
            }
        }
        """;

    // ─── DTOs ─────────────────────────────────────────────────────────────────

    private string SpecialistRequestDtoTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public readonly record struct {{method}}Request({{RecordParams(p)}});
        """;

    private string CommandResponseDtoTemplate(string method) => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public readonly record struct {{method}}Response(bool Success);
        """;

    private string QueryResultRecordTemplate(string method, IReadOnlyList<SpecialistParam> cols) => $$"""
        namespace {{ctx.NS}}.Domain.QueryResults;

        public readonly record struct {{method}}QueryResult({{RecordParams(cols)}});
        """;

    private string QueryResponseDtoTemplate(string method, IReadOnlyList<SpecialistParam> cols) => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public readonly record struct {{method}}Response({{RecordParams(cols)}});
        """;

    private string QueryMapperProfileTemplate(string method, bool paged)
    {
        var paginatedUsing   = paged ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n" : string.Empty;
        var paginatedMapping = paged
            ? $"\n        CreateMap<PaginatedQueryResult<{method}QueryResult>, PaginatedResponse<{method}Response>>();"
            : string.Empty;
        return $$"""
            using AutoMapper;
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Application.Mappers;

            public sealed class {{method}}MapperProfile : Profile
            {
                public {{method}}MapperProfile()
                {
                    CreateMap<{{method}}QueryResult, {{method}}Response>();{{paginatedMapping}}
                }
            }
            """;
    }

    // ─── HTTP Call ─────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> HttpCallSpecialistFiles(string method)
    {
        var feat           = Path.Combine(ctx.AppPath, Features, $"{ctx.Entity}Features", $"{method}Feature");
        var httpInterfaces = Path.Combine(ctx.AppPath, Interfaces, "HttpServices");
        var httpServices   = Path.Combine(ctx.InfraDataPath, "HttpServices");

        yield return (Path.Combine(feat, $"{method}Command.cs"),          HttpCallCommandTemplate(method));
        yield return (Path.Combine(feat, $"{method}CommandHandler.cs"),   HttpCallHandlerTemplate(method));
        yield return (Path.Combine(feat, $"{method}CommandValidator.cs"), HttpCallValidatorTemplate(method));
        yield return (Path.Combine(httpInterfaces, $"I{method}HttpService.cs"), HttpServiceInterfaceTemplate(method));
        yield return (Path.Combine(httpServices,   $"{method}HttpService.cs"),  HttpServiceTemplate(method));
    }

    private string HttpCallCommandTemplate(string method) => $$"""
        using MediatR;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Command() : IRequest<bool>;
        """;

    private string HttpCallHandlerTemplate(string method) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.Interfaces.HttpServices;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        internal sealed class {{method}}CommandHandler(I{{method}}HttpService {{ToCamel(method)}}HttpService)
            : IRequestHandler<{{method}}Command, bool>
        {
            public async Task<bool> Handle({{method}}Command request, CancellationToken cancellationToken)
                => await {{ToCamel(method)}}HttpService.ExecuteAsync(cancellationToken);
        }
        """;

    private string HttpCallValidatorTemplate(string method) => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed class {{method}}CommandValidator : AbstractValidator<{{method}}Command>
        {
            public {{method}}CommandValidator() { }
        }
        """;

    private string HttpServiceInterfaceTemplate(string method) => $$"""
        namespace {{ctx.NS}}.Application.Interfaces.HttpServices;

        public interface I{{method}}HttpService
        {
            Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
        }
        """;

    private string HttpServiceTemplate(string method) => $$"""
        using {{ctx.NS}}.Application.Interfaces.HttpServices;

        namespace {{ctx.NS}}.Infra.Data.HttpServices;

        public sealed class {{method}}HttpService(HttpClient httpClient) : I{{method}}HttpService
        {
            public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
        """;

    // ─── Test helpers ──────────────────────────────────────────────────────────

    private static string SpecialistTestValue(SpecialistParam p) => p.CsType switch
    {
        CsString   => "\"Test\"",
        "int"      => "1",
        "bool"     => "true",
        "decimal"  => "1.0m",
        "Guid"     => "Guid.NewGuid()",
        "DateTime" => "DateTime.UtcNow",
        "long"     => "1L",
        "double"   => "1.0",
        "float"    => "1.0f",
        "short"    => "(short)1",
        _          => "default"
    };

    private static string SpecialistTestArgs(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(SpecialistTestValue));

    private static string AnyArgsLeading(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0 ? string.Empty : string.Join(", ", p.Select(x => $"It.IsAny<{x.CsType}>()")) + ", ";

    private static string BuildSpecialistValidatorTests(IReadOnlyList<SpecialistParam> p, string typeName)
    {
        var sb = new StringBuilder();
        sb.Append($"[Fact]\n{I4}public void Validate_IsValid_WhenAllParamsAreProvided()");
        sb.Append($"\n{I4}{{\n{I8}var result = _validator.Validate(new {typeName}({SpecialistTestArgs(p)}));");
        sb.Append($"\n{I8}Assert.True(result.IsValid);\n{I4}}}");

        foreach (var param in p.Where(x => x.CsType is CsString or "Guid"))
        {
            var empty = param.CsType == CsString ? "\"\"" : "Guid.Empty";
            var args  = string.Join(", ", p.Select(x => x == param ? empty : SpecialistTestValue(x)));
            sb.Append($"\n\n{I4}[Fact]\n{I4}public void Validate_IsInvalid_When{param.PascalName}IsEmpty()");
            sb.Append($"\n{I4}{{\n{I8}var result = _validator.Validate(new {typeName}({args}));");
            sb.Append($"\n{I8}Assert.False(result.IsValid);");
            sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"{param.PascalName}\");");
            sb.Append($"\n{I4}}}");
        }

        return sb.ToString();
    }

    // ─── Query — Tests ─────────────────────────────────────────────────────────

    private string QueryHandlerTestsTemplate(string method, IReadOnlyList<SpecialistParam> p,
        IReadOnlyList<SpecialistParam> cols, bool paged)
    {
        var responseType   = paged ? $"PaginatedResponse<{method}Response>" : $"IReadOnlyList<{method}Response>";
        var colArgs        = SpecialistTestArgs(cols);
        var resultSetup      = paged
            ? $"new PaginatedQueryResult<{method}QueryResult>(1, 5, 1, [new({colArgs})])"
            : $"new List<{method}QueryResult> {{ new({colArgs}) }}";
        var paginatedUsing   = paged ? $"using {ctx.NS}.Application.DTOs.Base.Response;\n" : string.Empty;

        return $$"""
            using AutoMapper;
            using Moq;
            {{paginatedUsing}}using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
            using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;
            using {{ctx.NS}}.Domain.Interfaces.Services;
            using {{ctx.NS}}.Domain.QueryResults;

            namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

            public sealed class {{method}}QueryHandlerTests
            {
                private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
                private readonly Mock<IMapper> _mapperMock = new();
                private readonly {{method}}QueryHandler _handler;

                public {{method}}QueryHandlerTests()
                {
                    _handler = new {{method}}QueryHandler(_{{ctx.ECamel}}DomainServiceMock.Object, _mapperMock.Object);
                }

                [Fact]
                public async Task Handle_CallsService_WithCorrectParameters()
                {
                    var query = new {{method}}Query({{SpecialistTestArgs(p)}});
                    var serviceResult = {{resultSetup}};

                    _{{ctx.ECamel}}DomainServiceMock
                        .Setup(s => s.{{method}}Async({{AnyArgsLeading(p)}}It.IsAny<CancellationToken>()))
                        .ReturnsAsync(serviceResult);
                    _mapperMock
                        .Setup(m => m.Map<{{responseType}}>(serviceResult))
                        .Returns(default!);

                    await _handler.Handle(query, CancellationToken.None);

                    _{{ctx.ECamel}}DomainServiceMock.Verify(
                        s => s.{{method}}Async({{AnyArgsLeading(p)}}It.IsAny<CancellationToken>()),
                        Times.Once());
                }
            }
            """;
    }

    private string QueryValidatorTestsTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class {{method}}QueryValidatorTests
        {
            private readonly {{method}}QueryValidator _validator = new();

            {{BuildSpecialistValidatorTests(p, $"{method}Query")}}
        }
        """;

    private string QueryAppServiceTestsTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;
        using {{ctx.NS}}.Application.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Services;

        public sealed class {{ctx.Entity}}{{method}}AppServiceTests
        {
            private readonly Mock<IMediator> _mediatorMock = new();
            private readonly Mock<AutoMapper.IMapper> _mapperMock = new();
            private readonly {{ctx.Entity}}ApplicationService _service;

            public {{ctx.Entity}}{{method}}AppServiceTests()
            {
                _service = new {{ctx.Entity}}ApplicationService(_mediatorMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task {{method}}Async_SendsQuery_ToMediator()
            {
                var request = new {{method}}Request({{SpecialistTestArgs(p)}});

                await _service.{{method}}Async(request, CancellationToken.None);

                _mediatorMock.Verify(
                    m => m.Send(It.IsAny<{{method}}Query>(), It.IsAny<CancellationToken>()),
                    Times.Once());
            }
        }
        """;

    // ─── Command — Tests ───────────────────────────────────────────────────────

    private string CommandHandlerTestsTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class {{method}}CommandHandlerTests
        {
            private readonly Mock<I{{ctx.Entity}}DomainService> _{{ctx.ECamel}}DomainServiceMock = new();
            private readonly {{method}}CommandHandler _handler;

            public {{method}}CommandHandlerTests()
            {
                _handler = new {{method}}CommandHandler(_{{ctx.ECamel}}DomainServiceMock.Object);
            }

            [Fact]
            public async Task Handle_ReturnsSuccess_WhenCommandSucceeds()
            {
                var command = new {{method}}Command({{SpecialistTestArgs(p)}});
                _{{ctx.ECamel}}DomainServiceMock
                    .Setup(s => s.{{method}}Async({{AnyArgsLeading(p)}}It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.True(result.Success);
            }

            [Fact]
            public async Task Handle_ReturnsFailure_WhenCommandFails()
            {
                var command = new {{method}}Command({{SpecialistTestArgs(p)}});
                _{{ctx.ECamel}}DomainServiceMock
                    .Setup(s => s.{{method}}Async({{AnyArgsLeading(p)}}It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.False(result.Success);
            }
        }
        """;

    private string CommandValidatorTestsTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class {{method}}CommandValidatorTests
        {
            private readonly {{method}}CommandValidator _validator = new();

            {{BuildSpecialistValidatorTests(p, $"{method}Command")}}
        }
        """;

    private string CommandAppServiceTestsTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using Moq;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;
        using {{ctx.NS}}.Application.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Services;

        public sealed class {{ctx.Entity}}{{method}}AppServiceTests
        {
            private readonly Mock<IMediator> _mediatorMock = new();
            private readonly Mock<AutoMapper.IMapper> _mapperMock = new();
            private readonly {{ctx.Entity}}ApplicationService _service;

            public {{ctx.Entity}}{{method}}AppServiceTests()
            {
                _service = new {{ctx.Entity}}ApplicationService(_mediatorMock.Object, _mapperMock.Object);
            }

            [Fact]
            public async Task {{method}}Async_SendsCommand_ToMediator()
            {
                var request = new {{method}}Request({{SpecialistTestArgs(p)}});

                await _service.{{method}}Async(request, CancellationToken.None);

                _mediatorMock.Verify(
                    m => m.Send(It.IsAny<{{method}}Command>(), It.IsAny<CancellationToken>()),
                    Times.Once());
            }
        }
        """;
}
