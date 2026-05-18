namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    public IEnumerable<(string Path, string Content)> GetSpecialistFiles(string methodName, SpecialistType type) =>
        type switch
        {
            SpecialistType.Query    => QuerySpecialistFiles(methodName),
            SpecialistType.Command  => CommandSpecialistFiles(methodName),
            SpecialistType.HttpCall => HttpCallSpecialistFiles(methodName),
            _                      => []
        };

    // ─── Query ─────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> QuerySpecialistFiles(string method)
    {
        var feat = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");

        yield return (
            Path.Combine(ctx.DomainPath, "Interfaces", Services, $"I{ctx.Entity}DomainService.{method}.cs"),
            QueryInterfacePartial(method));
        yield return (
            Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.{method}.cs"),
            QueryServicePartial(method));
        yield return (Path.Combine(feat, $"{method}Query.cs"),          QuerySpecTemplate(method));
        yield return (Path.Combine(feat, $"{method}QueryHandler.cs"),   QueryHandlerSpecTemplate(method));
        yield return (Path.Combine(feat, $"{method}QueryValidator.cs"), QueryValidatorSpecTemplate(method));
    }

    private string QueryInterfacePartial(string method) => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public partial interface I{{ctx.Entity}}DomainService
        {
            Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(CancellationToken cancellationToken = default);
        }
        """;

    private string QueryServicePartial(string method) => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Services;

        public sealed partial class {{ctx.Entity}}DomainService
        {
            public async Task<IReadOnlyList<{{ctx.Entity}}>> {{method}}Async(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
        """;

    private string QuerySpecTemplate(string method) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Query() : IRequest<IReadOnlyList<{{ctx.Entity}}Response>>;
        """;

    private string QueryHandlerSpecTemplate(string method) => $$"""
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
                var result = await {{ctx.ECamel}}DomainService.{{method}}Async(cancellationToken);
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

    private IEnumerable<(string, string)> CommandSpecialistFiles(string method)
    {
        var feat = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");

        yield return (
            Path.Combine(ctx.DomainPath, "Interfaces", Services, $"I{ctx.Entity}DomainService.{method}.cs"),
            CommandInterfacePartial(method));
        yield return (
            Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.{method}.cs"),
            CommandServicePartial(method));
        yield return (Path.Combine(feat, $"{method}Command.cs"),          CommandSpecTemplate(method));
        yield return (Path.Combine(feat, $"{method}CommandHandler.cs"),   CommandHandlerSpecTemplate(method));
        yield return (Path.Combine(feat, $"{method}CommandValidator.cs"), CommandValidatorSpecTemplate(method));
    }

    private string CommandInterfacePartial(string method) => $$"""
        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public partial interface I{{ctx.Entity}}DomainService
        {
            Task<bool> {{method}}Async(int id, CancellationToken cancellationToken = default);
        }
        """;

    private string CommandServicePartial(string method) => $$"""
        namespace {{ctx.NS}}.Domain.Services;

        public sealed partial class {{ctx.Entity}}DomainService
        {
            public async Task<bool> {{method}}Async(int id, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
        """;

    private string CommandSpecTemplate(string method) => $$"""
        using MediatR;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed record {{method}}Command(int Id) : IRequest<bool>;
        """;

    private string CommandHandlerSpecTemplate(string method) => $$"""
        using MediatR;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        internal sealed class {{method}}CommandHandler(I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService)
            : IRequestHandler<{{method}}Command, bool>
        {
            public async Task<bool> Handle({{method}}Command request, CancellationToken cancellationToken)
                => await {{ctx.ECamel}}DomainService.{{method}}Async(request.Id, cancellationToken);
        }
        """;

    private string CommandValidatorSpecTemplate(string method) => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{method}}Feature;

        public sealed class {{method}}CommandValidator : AbstractValidator<{{method}}Command>
        {
            public {{method}}CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .GreaterThan(0);
            }
        }
        """;

    // ─── HTTP Call ─────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> HttpCallSpecialistFiles(string method)
    {
        var feat          = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features", $"{method}Feature");
        var httpInterfaces = Path.Combine(ctx.AppPath, "Interfaces", "HttpServices");
        var httpServices  = Path.Combine(ctx.InfraDataPath, "HttpServices");

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
