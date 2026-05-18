namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
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
        string.Join(", ", p.Select(x => $"{x.CsType} {x.CamelName}"));

    private static string MethodParamsLeading(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0 ? string.Empty : MethodParams(p) + ", ";

    private static string RecordParams(IReadOnlyList<SpecialistParam> p) =>
        string.Join(", ", p.Select(x => $"{x.CsType} {x.PascalName}"));

    private static string RequestCallArgs(IReadOnlyList<SpecialistParam> p) =>
        (p.Count == 0 ? string.Empty : string.Join(", ", p.Select(x => $"request.{x.PascalName}")) + ", ");

    private static string DapperAnon(IReadOnlyList<SpecialistParam> p) =>
        p.Count == 0
            ? "new { }"
            : $"new {{ {string.Join(", ", p.Select(x => $"{x.PascalName} = {x.CamelName}"))} }}";

    // ─── Query ─────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> QuerySpecialistFiles(SpecialistDefinition def)
    {
        var method = def.MethodName;
        var feat   = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");
        var sql    = SpecialistParam.ToParameterizedSql(def.Sql, def.Parameters);

        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Repositories, $"I{ctx.Entity}Repository.{method}.cs"),
            QueryRepositoryInterfacePartial(method, def.Parameters));
        yield return (
            Path.Combine(ctx.InfraDataPath, Repositories, $"{ctx.Entity}Repository.{method}.cs"),
            QueryRepositoryPartial(method, def.Parameters, sql));
        yield return (
            Path.Combine(ctx.DomainPath, Interfaces, Services, $"I{ctx.Entity}DomainService.{method}.cs"),
            QueryInterfacePartial(method, def.Parameters));
        yield return (
            Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.{method}.cs"),
            QueryServicePartial(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}Query.cs"),          QuerySpecTemplate(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}QueryHandler.cs"),   QueryHandlerSpecTemplate(method, def.Parameters));
        yield return (Path.Combine(feat, $"{method}QueryValidator.cs"), QueryValidatorSpecTemplate(method));
    }

    private string QueryRepositoryInterfacePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

        public partial interface I{{ctx.Entity}}Repository
        {
            Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
        }
        """;

    private string QueryRepositoryPartial(string method, IReadOnlyList<SpecialistParam> p, string sql) => $$"""
        using Dapper;
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed partial class {{ctx.Entity}}Repository
        {
            private const string {{method}}Sql = "{{sql.Replace("\"", "\\\"")}}";

            public async Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
            {
                var result = await dbSession.Connection.QueryAsync<{{ctx.Entity}}>(
                    {{method}}Sql, {{DapperAnon(p)}});
                return result.ToList();
            }
        }
        """;

    private string QueryInterfacePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public partial interface I{{ctx.Entity}}DomainService
        {
            Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default);
        }
        """;

    private string QueryServicePartial(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Services;

        public sealed partial class {{ctx.Entity}}DomainService
        {
            public async Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(
                {{MethodParamsLeading(p)}}CancellationToken cancellationToken = default)
                => await {{ctx.ECamel}}Repository.{{method}}Async(
                    {{MethodParamsLeading(p)}}cancellationToken);
        }
        """;

    private string QuerySpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Query({{RecordParams(p)}})
            : IRequest<IReadOnlyList<{{ctx.Entity}}Response>>;
        """;

    private string QueryHandlerSpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        internal sealed class {{method}}QueryHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<{{method}}Query, IReadOnlyList<{{ctx.Entity}}Response>>
        {
            public async Task<IReadOnlyList<{{ctx.Entity}}Response>>
                Handle({{method}}Query request, CancellationToken cancellationToken)
            {
                var result = await {{ctx.ECamel}}DomainService.{{method}}Async(
                    {{RequestCallArgs(p)}}cancellationToken);
                return mapper.Map<IReadOnlyList<{{ctx.Entity}}Response>>(result);
            }
        }
        """;

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
        var feat   = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");
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
                var affected = await dbSession.Connection.ExecuteAsync(
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
                    {{MethodParamsLeading(p)}}cancellationToken);
        }
        """;

    private string CommandSpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Command({{RecordParams(p)}}) : IRequest<bool>;
        """;

    private string CommandHandlerSpecTemplate(string method, IReadOnlyList<SpecialistParam> p) => $$"""
        using MediatR;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        internal sealed class {{method}}CommandHandler(I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService)
            : IRequestHandler<{{method}}Command, bool>
        {
            public async Task<bool> Handle({{method}}Command request, CancellationToken cancellationToken)
                => await {{ctx.ECamel}}DomainService.{{method}}Async(
                    {{RequestCallArgs(p)}}cancellationToken);
        }
        """;

    private string CommandValidatorSpecTemplate(string method, IReadOnlyList<SpecialistParam> p)
    {
        var rules = p
            .Where(x => x.CsType is "string" or "Guid")
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

    // ─── HTTP Call ─────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> HttpCallSpecialistFiles(string method)
    {
        var feat           = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");
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
}
