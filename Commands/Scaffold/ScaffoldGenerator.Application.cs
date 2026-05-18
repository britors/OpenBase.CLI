namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator
{
    private IEnumerable<(string, string)> ApplicationFiles()
    {
        var dtoReq = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Requests);
        var dtoRes = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Responses);
        var feat   = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features");

        yield return (Path.Combine(dtoReq, $"Create{ctx.Entity}Request.cs"), CreateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Update{ctx.Entity}Request.cs"), UpdateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Delete{ctx.Entity}Request.cs"), DeleteRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Find{ctx.Entity}ByIdRequest.cs"), FindByIdRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Get{ctx.Entity}Request.cs"), GetRequestTemplate());

        yield return (Path.Combine(dtoRes, $"{ctx.Entity}Response.cs"), ResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Create{ctx.Entity}Response.cs"), CreateResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Update{ctx.Entity}Response.cs"), UpdateResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Delete{ctx.Entity}Response.cs"), DeleteResponseTemplate());

        var create = Path.Combine(feat, $"Create{ctx.Entity}Feature");
        yield return (Path.Combine(create, $"Create{ctx.Entity}Command.cs"), CreateCommandTemplate());
        yield return (Path.Combine(create, $"Create{ctx.Entity}CommandHandler.cs"), CreateCommandHandlerTemplate());
        yield return (Path.Combine(create, $"Create{ctx.Entity}CommandValidator.cs"), CreateCommandValidatorTemplate());

        var delete = Path.Combine(feat, $"Delete{ctx.Entity}Feature");
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}Command.cs"), DeleteCommandTemplate());
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}CommandHandler.cs"), DeleteCommandHandlerTemplate());
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}CommandValidator.cs"), DeleteCommandValidatorTemplate());

        var findById = Path.Combine(feat, $"Find{ctx.Entity}ByIdFeature");
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQuery.cs"), FindByIdQueryTemplate());
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQueryHandler.cs"), FindByIdQueryHandlerTemplate());
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQueryValidator.cs"), FindByIdQueryValidatorTemplate());

        var get = Path.Combine(feat, $"Get{ctx.EPlural}Feature");
        yield return (Path.Combine(get, $"Get{ctx.Entity}Query.cs"), GetQueryTemplate());
        yield return (Path.Combine(get, $"Get{ctx.Entity}QueryHandler.cs"), GetQueryHandlerTemplate());
        yield return (Path.Combine(get, $"Get{ctx.Entity}QueryValidator.cs"), GetQueryValidatorTemplate());

        var update = Path.Combine(feat, $"Update{ctx.Entity}Feature");
        yield return (Path.Combine(update, $"Update{ctx.Entity}Command.cs"), UpdateCommandTemplate());
        yield return (Path.Combine(update, $"Update{ctx.Entity}CommandHandler.cs"), UpdateCommandHandlerTemplate());
        yield return (Path.Combine(update, $"Update{ctx.Entity}CommandValidator.cs"), UpdateCommandValidatorTemplate());

        yield return (Path.Combine(ctx.AppPath, "Interfaces", Services, $"I{ctx.Entity}ApplicationService.cs"), IApplicationServiceTemplate());
        yield return (Path.Combine(ctx.AppPath, "Mappers", $"{ctx.Entity}MapperProfile.cs"), MapperProfileTemplate());
        yield return (Path.Combine(ctx.AppPath, Services, $"{ctx.Entity}ApplicationService.cs"), ApplicationServiceTemplate());
    }


    private string DtoTemplate(string subNs, string typeName, string body) => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.{{subNs}};

        public readonly record struct {{typeName}}({{body}});
        """;

    private string CreateRequestTemplate()    => DtoTemplate(Requests,  $"Create{ctx.Entity}Request",    CreateParams());
    private string UpdateRequestTemplate()    => DtoTemplate(Requests,  $"Update{ctx.Entity}Request",    IdAndPropertiesParams());
    private string DeleteRequestTemplate()    => DtoTemplate(Requests,  $"Delete{ctx.Entity}Request",    IntId);
    private string FindByIdRequestTemplate()  => DtoTemplate(Requests,  $"Find{ctx.Entity}ByIdRequest",  IntId);
    private string ResponseTemplate()         => DtoTemplate(Responses, $"{ctx.Entity}Response",         IdAndPropertiesParams());
    private string CreateResponseTemplate()   => DtoTemplate(Responses, $"Create{ctx.Entity}Response",   IdAndPropertiesParams());
    private string UpdateResponseTemplate()   => DtoTemplate(Responses, $"Update{ctx.Entity}Response",   IdAndPropertiesParams());
    private string DeleteResponseTemplate()   => DtoTemplate(Responses, $"Delete{ctx.Entity}Response",   "bool Success");

    private string GetRequestTemplate()
    {
        var filterPart = FilterableProperties.Any() ? FilterParamsWithDefaults() + ", " : "";
        return DtoTemplate(Requests, $"Get{ctx.Entity}Request", $"{filterPart}int Page = 1, int PageSize = 5");
    }


    private string CommandTemplate(string feature, string name, string parameters, string response) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{feature}};

        public sealed record {{name}}({{parameters}}) : IRequest<{{response}}?>;
        """;

    private string CreateCommandTemplate() => CommandTemplate(
        $"Create{ctx.Entity}Feature", $"Create{ctx.Entity}Command", CreateParams(), $"Create{ctx.Entity}Response");

    private string DeleteCommandTemplate() => CommandTemplate(
        $"Delete{ctx.Entity}Feature", $"Delete{ctx.Entity}Command", IntId, $"Delete{ctx.Entity}Response");

    private string UpdateCommandTemplate() => CommandTemplate(
        $"Update{ctx.Entity}Feature", $"Update{ctx.Entity}Command", IdAndPropertiesParams(), $"Update{ctx.Entity}Response");


    private string MapperCommandHandlerTemplate(string verb, string serviceMethod, string resultPrefix) => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{verb}}{{ctx.Entity}}Feature;

        public sealed class {{verb}}{{ctx.Entity}}CommandHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<{{verb}}{{ctx.Entity}}Command, {{verb}}{{ctx.Entity}}Response?>
        {
            public async Task<{{verb}}{{ctx.Entity}}Response?>
                Handle({{verb}}{{ctx.Entity}}Command request, CancellationToken cancellationToken)
            {
                var {{ctx.ECamel}} = mapper.Map<{{ctx.Entity}}>(request);
                var {{resultPrefix}}{{ctx.Entity}} = await {{ctx.ECamel}}DomainService.{{serviceMethod}}({{ctx.ECamel}}, cancellationToken);
                return mapper.Map<{{verb}}{{ctx.Entity}}Response>({{resultPrefix}}{{ctx.Entity}});
            }
        }
        """;

    private string CreateCommandHandlerTemplate() =>
        MapperCommandHandlerTemplate("Create", "AddAsync", "new");

    private string DeleteCommandHandlerTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;

        internal sealed class Delete{{ctx.Entity}}CommandHandler(I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService)
            : IRequestHandler<Delete{{ctx.Entity}}Command, Delete{{ctx.Entity}}Response?>
        {
            public async Task<Delete{{ctx.Entity}}Response?>
                Handle(Delete{{ctx.Entity}}Command request, CancellationToken cancellationToken)
            {
                var success = await {{ctx.ECamel}}DomainService.RemoveByIdAsync(request.Id, cancellationToken);
                return new Delete{{ctx.Entity}}Response(success);
            }
        }
        """;

    private string FindByIdQueryHandlerTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        internal sealed class Find{{ctx.Entity}}ByIdQueryHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<Find{{ctx.Entity}}ByIdQuery, {{ctx.Entity}}Response>
        {
            public async Task<{{ctx.Entity}}Response>
                Handle(Find{{ctx.Entity}}ByIdQuery request, CancellationToken cancellationToken)
            {
                var result = await {{ctx.ECamel}}DomainService.GetByIdAsync(request.Id, cancellationToken);
                return mapper.Map<{{ctx.Entity}}Response>(result);
            }
        }
        """;

    private string GetQueryHandlerTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

        internal sealed class Get{{ctx.Entity}}QueryHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<Get{{ctx.Entity}}Query, PaginatedResponse<{{ctx.Entity}}Response>>
        {
            public async Task<PaginatedResponse<{{ctx.Entity}}Response>>
                Handle(Get{{ctx.Entity}}Query request, CancellationToken cancellationToken)
            {
                var queryResult = await {{ctx.ECamel}}DomainService.FindByArgumentsPagedAsync(
                    {{FindByArgumentsCallArgs()}});
                return mapper.Map<PaginatedResponse<{{ctx.Entity}}Response>>(queryResult);
            }
        }
        """;

    private string UpdateCommandHandlerTemplate() =>
        MapperCommandHandlerTemplate("Update", "UpdateAsync", "updated");


    private string FindByIdQueryTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        public sealed record Find{{ctx.Entity}}ByIdQuery(int Id) : IRequest<{{ctx.Entity}}Response>;
        """;

    private string GetQueryTemplate()
    {
        var filterPart = FilterableProperties.Any() ? FilterParamsNoDefaults() + ", " : "";
        return $$"""
            using MediatR;
            using {{ctx.NS}}.Application.DTOs.Base.Response;
            using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

            namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

            public sealed record Get{{ctx.Entity}}Query({{filterPart}}int Page, int PageSize)
                : IRequest<PaginatedResponse<{{ctx.Entity}}Response>>;
            """;
    }


    private string ValidatorTemplate(string feature, string typeName, string rules) => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{feature}};

        public sealed class {{typeName}}Validator : AbstractValidator<{{typeName}}>
        {
            public {{typeName}}Validator()
            {
                {{rules}}
            }
        }
        """;

    private string CreateCommandValidatorTemplate() => ValidatorTemplate(
        $"Create{ctx.Entity}Feature", $"Create{ctx.Entity}Command", CreateValidatorRules());

    private string DeleteCommandValidatorTemplate() => ValidatorTemplate(
        $"Delete{ctx.Entity}Feature", $"Delete{ctx.Entity}Command",
        $"RuleFor(x => x.Id)\n{I12}.NotEmpty()\n{I12}.NotNull();");

    private string FindByIdQueryValidatorTemplate() => ValidatorTemplate(
        $"Find{ctx.Entity}ByIdFeature", $"Find{ctx.Entity}ByIdQuery",
        "RuleFor(x => x.Id).NotEmpty();");

    private string GetQueryValidatorTemplate() => ValidatorTemplate(
        $"Get{ctx.EPlural}Feature", $"Get{ctx.Entity}Query",
        $"RuleFor(x => x.Page)\n{I12}.GreaterThanOrEqualTo(1)\n{I12}.WithMessage(\"O número da página deve ser maior ou igual a 1.\");\n\n{I8}RuleFor(x => x.PageSize)\n{I12}.GreaterThanOrEqualTo(5)\n{I12}.WithMessage(\"O tamanho da página deve ser maior ou igual a 5.\");");

    private string UpdateCommandValidatorTemplate() => ValidatorTemplate(
        $"Update{ctx.Entity}Feature", $"Update{ctx.Entity}Command", UpdateValidatorRules());


    private string IApplicationServiceTemplate() => $$"""
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Interfaces.Base;

        namespace {{ctx.NS}}.Application.Interfaces.Services;

        public partial interface I{{ctx.Entity}}ApplicationService : IApplicationService
        {
            Task<Create{{ctx.Entity}}Response?> CreateAsync(Create{{ctx.Entity}}Request request, CancellationToken cancellationToken);
            Task<Update{{ctx.Entity}}Response?> UpdateAsync(Update{{ctx.Entity}}Request request, CancellationToken cancellationToken);
            Task<Delete{{ctx.Entity}}Response?> DeleteAsync(Delete{{ctx.Entity}}Request request, CancellationToken cancellationToken);
            Task<{{ctx.Entity}}Response> GetByIdAsync(Find{{ctx.Entity}}ByIdRequest request, CancellationToken cancellationToken);
            Task<PaginatedResponse<{{ctx.Entity}}Response>> GetAsync(Get{{ctx.Entity}}Request request, CancellationToken cancellationToken);
        }
        """;

    private string MapperProfileTemplate() => $$"""
        using AutoMapper;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.QueryResults;

        namespace {{ctx.NS}}.Application.Mappers;

        public sealed class {{ctx.Entity}}MapperProfile : Profile
        {
            public {{ctx.Entity}}MapperProfile()
            {
                CreateMap<Get{{ctx.Entity}}Request, Get{{ctx.Entity}}Query>();
                CreateMap<Find{{ctx.Entity}}ByIdRequest, Find{{ctx.Entity}}ByIdQuery>();
                CreateMap<Update{{ctx.Entity}}Request, Update{{ctx.Entity}}Command>();
                CreateMap<Update{{ctx.Entity}}Command, {{ctx.Entity}}>();
                CreateMap<Create{{ctx.Entity}}Request, Create{{ctx.Entity}}Command>();
                CreateMap<Create{{ctx.Entity}}Command, {{ctx.Entity}}>();
                CreateMap<Delete{{ctx.Entity}}Request, Delete{{ctx.Entity}}Command>();
                CreateMap<{{ctx.Entity}}, {{ctx.Entity}}Response>();
                CreateMap<PaginatedQueryResult<{{ctx.Entity}}>, PaginatedResponse<{{ctx.Entity}}Response>>();
                CreateMap<{{ctx.Entity}}, Update{{ctx.Entity}}Response>();
                CreateMap<{{ctx.Entity}}, Create{{ctx.Entity}}Response>();
            }
        }
        """;

    private string ApplicationServiceTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Services;

        public sealed partial class {{ctx.Entity}}ApplicationService(IMediator mediator, IMapper mapper) : I{{ctx.Entity}}ApplicationService
        {
            public async Task<Create{{ctx.Entity}}Response?> CreateAsync(Create{{ctx.Entity}}Request request, CancellationToken cancellationToken)
            {
                var command = mapper.Map<Create{{ctx.Entity}}Command>(request);
                return await mediator.Send(command, cancellationToken);
            }

            public async Task<Update{{ctx.Entity}}Response?> UpdateAsync(Update{{ctx.Entity}}Request request, CancellationToken cancellationToken)
            {
                var command = mapper.Map<Update{{ctx.Entity}}Command>(request);
                return await mediator.Send(command, cancellationToken);
            }

            public async Task<Delete{{ctx.Entity}}Response?> DeleteAsync(Delete{{ctx.Entity}}Request request, CancellationToken cancellationToken)
            {
                var command = mapper.Map<Delete{{ctx.Entity}}Command>(request);
                return await mediator.Send(command, cancellationToken);
            }

            public async Task<{{ctx.Entity}}Response> GetByIdAsync(Find{{ctx.Entity}}ByIdRequest request, CancellationToken cancellationToken)
            {
                var query = mapper.Map<Find{{ctx.Entity}}ByIdQuery>(request);
                return await mediator.Send(query, cancellationToken);
            }

            public async Task<PaginatedResponse<{{ctx.Entity}}Response>> GetAsync(Get{{ctx.Entity}}Request request, CancellationToken cancellationToken)
            {
                var query = mapper.Map<Get{{ctx.Entity}}Query>(request);
                return await mediator.Send(query, cancellationToken);
            }
        }
        """;
}
