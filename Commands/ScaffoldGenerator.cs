namespace OpenBase.CLI.Commands;

public sealed record ScaffoldContext(string Entity, string RootNamespace, string SolutionDir)
{
    public string ECamel => char.ToLowerInvariant(Entity[0]) + Entity[1..];
    public string EPlural => Entity + "s";
    public string ELower => Entity.ToLowerInvariant();
    public string NS => RootNamespace;

    private string Src => Path.Combine(SolutionDir, "src");
    public string DomainPath => Path.Combine(Src, $"{NS}.Domain");
    public string AppPath => Path.Combine(Src, $"{NS}.Application");
    public string InfraContextPath => Path.Combine(Src, $"{NS}.Infra.Data.Context");
    public string InfraDataPath => Path.Combine(Src, $"{NS}.Infra.Data");
    public string PresentationPath => Path.Combine(Src, $"{NS}.Presentation.Api");
}

public sealed class ScaffoldGenerator(ScaffoldContext ctx)
{
    public IEnumerable<(string Path, string Content)> GetFiles() =>
        DomainFiles()
            .Concat(ApplicationFiles())
            .Concat(InfrastructureFiles())
            .Concat(PresentationFiles());

    // ── Domain ────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> DomainFiles()
    {
        yield return (Path.Combine(ctx.DomainPath, "Entities", $"{ctx.Entity}.cs"), EntityTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", "Repositories", $"I{ctx.Entity}Repository.cs"), IRepositoryTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", "Services", $"I{ctx.Entity}DomainService.cs"), IDomainServiceTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Services", $"{ctx.Entity}DomainService.cs"), DomainServiceTemplate());
    }

    private string EntityTemplate() => $$"""
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Domain.Entities;

        public sealed class {{ctx.Entity}} : IEntityOrQueryResult
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
        }
        """;

    private string IRepositoryTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Domain.Interfaces.Repositories;

        public interface I{{ctx.Entity}}Repository : IRepositoryBase<{{ctx.Entity}}>
        {
        }
        """;

    private string IDomainServiceTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.QueryResults;

        namespace {{ctx.NS}}.Domain.Interfaces.Services;

        public interface I{{ctx.Entity}}DomainService : IDomainService<{{ctx.Entity}}, int>
        {
            Task<PaginatedQueryResult<{{ctx.Entity}}>> FindByNamePagedAsync(
                string name, int page, int pageSize, CancellationToken cancellationToken);
        }
        """;

    private string DomainServiceTemplate() => $$"""
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Domain.Interfaces.Services;
        using {{ctx.NS}}.Domain.QueryResults;
        using System.Linq.Expressions;

        namespace {{ctx.NS}}.Domain.Services;

        public sealed class {{ctx.Entity}}DomainService(I{{ctx.Entity}}Repository {{ctx.ECamel}}Repository)
            : DomainService<{{ctx.Entity}}, int>({{ctx.ECamel}}Repository), I{{ctx.Entity}}DomainService
        {
            public async Task<PaginatedQueryResult<{{ctx.Entity}}>> FindByNamePagedAsync(
                string name, int page, int pageSize, CancellationToken cancellationToken)
            {
                Expression<Func<{{ctx.Entity}}, bool>>? query = null;
                if (!string.IsNullOrWhiteSpace(name))
                    query = x => x.Name.Contains(name);

                var totalRecords = await {{ctx.ECamel}}Repository.CountAsync(cancellationToken, query);
                var resultPaginated = await {{ctx.ECamel}}Repository.FindAsync(
                    cancellationToken,
                    noTracking: true,
                    query,
                    pageNumber: page,
                    pageSize: pageSize);

                return new PaginatedQueryResult<{{ctx.Entity}}>(page, pageSize, totalRecords, resultPaginated);
            }
        }
        """;

    // ── Application ───────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> ApplicationFiles()
    {
        var dtoReq = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, "Requests");
        var dtoRes = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, "Responses");
        var feat = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features");

        // DTOs – Requests
        yield return (Path.Combine(dtoReq, $"Create{ctx.Entity}Request.cs"), CreateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Update{ctx.Entity}Request.cs"), UpdateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Delete{ctx.Entity}Request.cs"), DeleteRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Find{ctx.Entity}ByIdRequest.cs"), FindByIdRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Get{ctx.Entity}Request.cs"), GetRequestTemplate());

        // DTOs – Responses
        yield return (Path.Combine(dtoRes, $"{ctx.Entity}Response.cs"), ResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Create{ctx.Entity}Response.cs"), CreateResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Update{ctx.Entity}Response.cs"), UpdateResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Delete{ctx.Entity}Response.cs"), DeleteResponseTemplate());

        // Create feature
        var create = Path.Combine(feat, $"Create{ctx.Entity}Feature");
        yield return (Path.Combine(create, $"Create{ctx.Entity}Command.cs"), CreateCommandTemplate());
        yield return (Path.Combine(create, $"Create{ctx.Entity}CommandHandler.cs"), CreateCommandHandlerTemplate());
        yield return (Path.Combine(create, $"Create{ctx.Entity}CommandValidator.cs"), CreateCommandValidatorTemplate());

        // Delete feature
        var delete = Path.Combine(feat, $"Delete{ctx.Entity}Feature");
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}Command.cs"), DeleteCommandTemplate());
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}CommandHandler.cs"), DeleteCommandHandlerTemplate());
        yield return (Path.Combine(delete, $"Delete{ctx.Entity}CommandValidator.cs"), DeleteCommandValidatorTemplate());

        // FindById feature
        var findById = Path.Combine(feat, $"Find{ctx.Entity}ByIdFeature");
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQuery.cs"), FindByIdQueryTemplate());
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQueryHandler.cs"), FindByIdQueryHandlerTemplate());
        yield return (Path.Combine(findById, $"Find{ctx.Entity}ByIdQueryValidator.cs"), FindByIdQueryValidatorTemplate());

        // Get (paginated) feature
        var get = Path.Combine(feat, $"Get{ctx.EPlural}Feature");
        yield return (Path.Combine(get, $"Get{ctx.Entity}Query.cs"), GetQueryTemplate());
        yield return (Path.Combine(get, $"Get{ctx.Entity}QueryHandler.cs"), GetQueryHandlerTemplate());
        yield return (Path.Combine(get, $"Get{ctx.Entity}QueryValidator.cs"), GetQueryValidatorTemplate());

        // Update feature
        var update = Path.Combine(feat, $"Update{ctx.Entity}Feature");
        yield return (Path.Combine(update, $"Update{ctx.Entity}Command.cs"), UpdateCommandTemplate());
        yield return (Path.Combine(update, $"Update{ctx.Entity}CommandHandler.cs"), UpdateCommandHandlerTemplate());
        yield return (Path.Combine(update, $"Update{ctx.Entity}CommandValidator.cs"), UpdateCommandValidatorTemplate());

        // Application service
        yield return (Path.Combine(ctx.AppPath, "Interfaces", "Services", $"I{ctx.Entity}ApplicationService.cs"), IApplicationServiceTemplate());
        yield return (Path.Combine(ctx.AppPath, "Mappers", $"{ctx.Entity}MapperProfile.cs"), MapperProfileTemplate());
        yield return (Path.Combine(ctx.AppPath, "Services", $"{ctx.Entity}ApplicationService.cs"), ApplicationServiceTemplate());
    }

    private string CreateRequestTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public sealed record Create{{ctx.Entity}}Request(string Name);
        """;

    private string UpdateRequestTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public sealed record Update{{ctx.Entity}}Request(int Id, string Name);
        """;

    private string DeleteRequestTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public sealed record Delete{{ctx.Entity}}Request(int Id);
        """;

    private string FindByIdRequestTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public sealed record Find{{ctx.Entity}}ByIdRequest(int Id);
        """;

    private string GetRequestTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;

        public sealed record Get{{ctx.Entity}}Request(string Name = "", int Page = 1, int PageSize = 5);
        """;

    private string ResponseTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public sealed record {{ctx.Entity}}Response(int Id, string Name);
        """;

    private string CreateResponseTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public sealed record Create{{ctx.Entity}}Response(int Id, string Name);
        """;

    private string UpdateResponseTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public sealed record Update{{ctx.Entity}}Response(int Id, string Name);
        """;

    private string DeleteResponseTemplate() => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        public sealed record Delete{{ctx.Entity}}Response(bool Success);
        """;

    private string CreateCommandTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;

        public sealed record Create{{ctx.Entity}}Command(string Name) : IRequest<Create{{ctx.Entity}}Response?>;
        """;

    private string CreateCommandHandlerTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;

        public sealed class Create{{ctx.Entity}}CommandHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<Create{{ctx.Entity}}Command, Create{{ctx.Entity}}Response?>
        {
            public async Task<Create{{ctx.Entity}}Response?>
                Handle(Create{{ctx.Entity}}Command request, CancellationToken cancellationToken)
            {
                var {{ctx.ECamel}} = mapper.Map<{{ctx.Entity}}>(request);
                var new{{ctx.Entity}} = await {{ctx.ECamel}}DomainService.AddAsync({{ctx.ECamel}}, cancellationToken);
                return mapper.Map<Create{{ctx.Entity}}Response>(new{{ctx.Entity}});
            }
        }
        """;

    private string CreateCommandValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;

        public sealed class Create{{ctx.Entity}}CommandValidator : AbstractValidator<Create{{ctx.Entity}}Command>
        {
            public Create{{ctx.Entity}}CommandValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .MinimumLength(1)
                    .MaximumLength(255);
            }
        }
        """;

    private string DeleteCommandTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;

        public sealed record Delete{{ctx.Entity}}Command(int Id) : IRequest<Delete{{ctx.Entity}}Response?>;
        """;

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

    private string DeleteCommandValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;

        public sealed class Delete{{ctx.Entity}}CommandValidator : AbstractValidator<Delete{{ctx.Entity}}Command>
        {
            public Delete{{ctx.Entity}}CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .NotNull();
            }
        }
        """;

    private string FindByIdQueryTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        public sealed record Find{{ctx.Entity}}ByIdQuery(int Id) : IRequest<{{ctx.Entity}}Response>;
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

    private string FindByIdQueryValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        public sealed class Find{{ctx.Entity}}ByIdQueryValidator : AbstractValidator<Find{{ctx.Entity}}ByIdQuery>
        {
            public Find{{ctx.Entity}}ByIdQueryValidator()
            {
                RuleFor(x => x.Id).NotEmpty();
            }
        }
        """;

    private string GetQueryTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

        public sealed record Get{{ctx.Entity}}Query(string Name, int Page, int PageSize)
            : IRequest<PaginatedResponse<{{ctx.Entity}}Response>>;
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
                var queryResult = await {{ctx.ECamel}}DomainService.FindByNamePagedAsync(
                    request.Name, request.Page, request.PageSize, cancellationToken);
                return mapper.Map<PaginatedResponse<{{ctx.Entity}}Response>>(queryResult);
            }
        }
        """;

    private string GetQueryValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

        public sealed class Get{{ctx.Entity}}QueryValidator : AbstractValidator<Get{{ctx.Entity}}Query>
        {
            public Get{{ctx.Entity}}QueryValidator()
            {
                RuleFor(x => x.Page)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("O número da página deve ser maior ou igual a 1.");

                RuleFor(x => x.PageSize)
                    .GreaterThanOrEqualTo(5)
                    .WithMessage("O tamanho da página deve ser maior ou igual a 5.");
            }
        }
        """;

    private string UpdateCommandTemplate() => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;

        public sealed record Update{{ctx.Entity}}Command(int Id, string Name) : IRequest<Update{{ctx.Entity}}Response?>;
        """;

    private string UpdateCommandHandlerTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;

        internal sealed class Update{{ctx.Entity}}CommandHandler(
                I{{ctx.Entity}}DomainService {{ctx.ECamel}}DomainService,
                IMapper mapper)
            : IRequestHandler<Update{{ctx.Entity}}Command, Update{{ctx.Entity}}Response?>
        {
            public async Task<Update{{ctx.Entity}}Response?>
                Handle(Update{{ctx.Entity}}Command request, CancellationToken cancellationToken)
            {
                var {{ctx.ECamel}} = mapper.Map<{{ctx.Entity}}>(request);
                var updated{{ctx.Entity}} = await {{ctx.ECamel}}DomainService.UpdateAsync({{ctx.ECamel}}, cancellationToken);
                return mapper.Map<Update{{ctx.Entity}}Response>(updated{{ctx.Entity}});
            }
        }
        """;

    private string UpdateCommandValidatorTemplate() => $$"""
        using FluentValidation;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;

        public sealed class Update{{ctx.Entity}}CommandValidator : AbstractValidator<Update{{ctx.Entity}}Command>
        {
            public Update{{ctx.Entity}}CommandValidator()
            {
                RuleFor(x => x.Id).NotEmpty();

                RuleFor(x => x.Name)
                    .MinimumLength(1)
                    .MaximumLength(255)
                    .When(x => !string.IsNullOrWhiteSpace(x.Name));
            }
        }
        """;

    private string IApplicationServiceTemplate() => $$"""
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Interfaces.Base;

        namespace {{ctx.NS}}.Application.Interfaces.Services;

        public interface I{{ctx.Entity}}ApplicationService : IApplicationService
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

        public sealed class {{ctx.Entity}}ApplicationService(IMediator mediator, IMapper mapper) : I{{ctx.Entity}}ApplicationService
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

    // ── Infrastructure ────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> InfrastructureFiles()
    {
        yield return (
            Path.Combine(ctx.InfraContextPath, "Configurations", $"{ctx.Entity}Configuration.cs"),
            EfConfigurationTemplate());
        yield return (
            Path.Combine(ctx.InfraDataPath, "Repositories", $"{ctx.Entity}Repository.cs"),
            RepositoryTemplate());
    }

    private string EfConfigurationTemplate() => $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{ctx.NS}}.Domain.Entities;

        namespace {{ctx.NS}}.Infra.Data.Context.Configurations;

        internal sealed class {{ctx.Entity}}Configuration : IEntityTypeConfiguration<{{ctx.Entity}}>
        {
            public void Configure(EntityTypeBuilder<{{ctx.Entity}}> builder)
            {
                builder.ToTable("{{ctx.EPlural}}");

                builder.HasKey(x => x.Id);

                builder.Property(x => x.Id).HasColumnName("Id");

                builder.Property(x => x.Name)
                    .HasColumnName("Name")
                    .HasMaxLength(255)
                    .IsRequired();
            }
        }
        """;

    private string RepositoryTemplate() => $$"""
        using Microsoft.Extensions.Logging;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Infra.Data.Context;

        namespace {{ctx.NS}}.Infra.Data.Repositories;

        public sealed class {{ctx.Entity}}Repository(
            DbSession dbSession,
            ILogger<RepositoryBase<{{ctx.Entity}}>> logger,
            OneBaseDataBaseContext context)
            : RepositoryBase<{{ctx.Entity}}>(dbSession, logger, context), I{{ctx.Entity}}Repository, IDataRepository
        {
        }
        """;

    // ── Presentation ──────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> PresentationFiles()
    {
        yield return (
            Path.Combine(ctx.PresentationPath, "Controllers", $"{ctx.Entity}Controller.cs"),
            ControllerTemplate());
    }

    private string ControllerTemplate() => $$"""
        using Microsoft.AspNetCore.Mvc;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.Interfaces.Services;

        namespace {{ctx.NS}}.Presentation.Api.Controllers;

        [ApiController]
        [Route("api/{{ctx.ELower}}")]
        [Produces("application/json")]
        public class {{ctx.Entity}}Controller(I{{ctx.Entity}}ApplicationService {{ctx.ECamel}}ApplicationService)
            : ControllerBase
        {
            /// <summary>Cria um(a) {{ctx.Entity}}.</summary>
            [HttpPost]
            [ProducesResponseType(StatusCodes.Status201Created)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
            public async Task<IActionResult> CreateAsync(
                [FromBody] Create{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = result!.Id }, result);
            }

            /// <summary>Remove um(a) {{ctx.Entity}}.</summary>
            [HttpDelete("{id:int}")]
            [ProducesResponseType(StatusCodes.Status204NoContent)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            public async Task<IActionResult> DeleteAsync(
                [FromRoute] int id,
                CancellationToken cancellationToken = default)
            {
                var request = new Delete{{ctx.Entity}}Request(id);
                var result = await {{ctx.ECamel}}ApplicationService.DeleteAsync(request, cancellationToken);

                if (result is null)
                    return NotFound();

                return NoContent();
            }

            /// <summary>Atualiza um(a) {{ctx.Entity}}.</summary>
            [HttpPut("{id:int}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
            public async Task<IActionResult> UpdateAsync(
                [FromRoute] int id,
                [FromBody] Update{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var requestWithId = request with { Id = id };
                var result = await {{ctx.ECamel}}ApplicationService.UpdateAsync(requestWithId, cancellationToken);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }

            /// <summary>Lista {{ctx.EPlural}} com paginação.</summary>
            [HttpGet]
            [ProducesResponseType(StatusCodes.Status200OK)]
            public async Task<IActionResult> GetAsync(
                [FromQuery] Get{{ctx.Entity}}Request request,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.GetAsync(request, cancellationToken);
                return Ok(result);
            }

            /// <summary>Busca um(a) {{ctx.Entity}} pelo id.</summary>
            [HttpGet("{id:int}")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
            public async Task<IActionResult> GetByIdAsync(
                [FromRoute] int id,
                CancellationToken cancellationToken = default)
            {
                var result = await {{ctx.ECamel}}ApplicationService.GetByIdAsync(
                    new Find{{ctx.Entity}}ByIdRequest(id), cancellationToken);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }
        }
        """;
}
