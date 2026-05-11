using System.Text;
using OpenBase.CLI.Helpers;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands;

public sealed record ScaffoldContext(string Entity, string RootNamespace, string SolutionDir)
{
    public string ECamel => char.ToLowerInvariant(Entity[0]) + Entity[1..];
    public string EPlural => Entity + "s";
    public string ELower => Entity.ToLowerInvariant();
    public string NS => RootNamespace;

    public IReadOnlyList<EntityProperty> Properties { get; init; } = DefaultProperties;
    public DbFlavor DbFlavor { get; init; } = DbFlavor.SqlServer;

    public EntityProperty? FilterProperty =>
        Properties.FirstOrDefault(p => p.IsStringType && p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
        ?? Properties.FirstOrDefault(p => p.IsStringType);

    private string Src => Path.Combine(SolutionDir, "src");
    public string DomainPath => Path.Combine(Src, $"{NS}.Domain");
    public string AppPath => Path.Combine(Src, $"{NS}.Application");
    public string InfraContextPath => Path.Combine(Src, $"{NS}.Infra.Data.Context");
    public string InfraDataPath => Path.Combine(Src, $"{NS}.Infra.Data");
    public string PresentationPath => Path.Combine(Src, $"{NS}.Presentation.Api");
    public string TestsPath => Path.Combine(SolutionDir, "tests", $"{NS}.Tests.Unit");
    public string TestsCsprojPath => Path.Combine(TestsPath, $"{NS}.Tests.Unit.csproj");

    private static readonly IReadOnlyList<EntityProperty> DefaultProperties =
        [new EntityProperty("Name", "string", true)];
}

public sealed class ScaffoldGenerator(ScaffoldContext ctx)
{
    private const string Services  = "Services";
    private const string Requests  = "Requests";
    private const string Responses = "Responses";

    // Output indentation levels (verbatim in generated files)
    private const string I4  = "    ";
    private const string I8  = "        ";
    private const string I12 = "            ";

    public IEnumerable<(string Path, string Content)> GetFiles() =>
        DomainFiles()
            .Concat(ApplicationFiles())
            .Concat(InfrastructureFiles())
            .Concat(PresentationFiles())
            .Concat(TestFiles());

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string EntityPropertyDeclarations() =>
        string.Join($"\n{I4}", ctx.Properties.Select(PropertyDeclaration));

    private static string PropertyDeclaration(EntityProperty p) =>
        p.IsStringType && p.IsRequired
            ? $"public {p.ActualCsType} {p.Name} {{ get; init; }} = string.Empty;"
            : $"public {p.ActualCsType} {p.Name} {{ get; init; }}";

    private string CreateParams() =>
        string.Join(", ", ctx.Properties.Select(p => $"{p.ActualCsType} {p.Name}"));

    private string IdAndPropertiesParams() =>
        ctx.Properties.Count == 0 ? "int Id" : $"int Id, {CreateParams()}";

    private string EfPropertyBlocks()
    {
        if (ctx.Properties.Count == 0) return string.Empty;
        return $"\n\n{I8}" + string.Join($"\n\n{I8}", ctx.Properties.Select(BuildEfBlock));
    }

    private string BuildEfBlock(EntityProperty p)
    {
        var sb = new StringBuilder();
        sb.Append($"builder.Property(x => x.{p.Name})");
        sb.Append($"\n{I12}.HasColumnName(\"{p.Name}\")");
        if (p.IsStringType) sb.Append($"\n{I12}.HasMaxLength(255)");
        if (p.CsType == "JsonDocument" && ctx.DbFlavor == DbFlavor.Postgres)
            sb.Append($"\n{I12}.HasColumnType(\"jsonb\")");
        if ((p.IsStringType || p.CsType == "byte[]") && p.IsRequired)
            sb.Append($"\n{I12}.IsRequired()");
        sb.Append(";");
        return sb.ToString();
    }

    private string DomainServiceFilterBody() =>
        ctx.FilterProperty is null ? string.Empty
            : $"\n{I8}if (!string.IsNullOrWhiteSpace(name))\n{I12}query = x => x.{ctx.FilterProperty.Name}.Contains(name);";

    private string CreateValidatorRules()
    {
        var rules = ctx.Properties.SelectMany(CreateRulesFor).ToList();
        return rules.Count == 0 ? string.Empty : string.Join($"\n\n{I8}", rules);
    }

    private string UpdateValidatorRules()
    {
        var rules = new List<string> { $"RuleFor(x => x.Id).NotEmpty().NotNull();" };
        rules.AddRange(ctx.Properties.Where(p => p.IsStringType).Select(BuildUpdateStringRule));
        return string.Join($"\n\n{I8}", rules);
    }

    private static IEnumerable<string> CreateRulesFor(EntityProperty p)
    {
        if (p.IsStringType && p.IsRequired)
            yield return $"RuleFor(x => x.{p.Name})\n{I12}.NotEmpty()\n{I12}.MinimumLength(1)\n{I12}.MaximumLength(255);";
        else if (p.CsType == "Guid" && p.IsRequired)
            yield return $"RuleFor(x => x.{p.Name}).NotEmpty();";
    }

    private static string BuildUpdateStringRule(EntityProperty p) =>
        $"RuleFor(x => x.{p.Name})\n{I12}.MinimumLength(1)\n{I12}.MaximumLength(255)\n{I12}.When(x => !string.IsNullOrWhiteSpace(x.{p.Name}));";

    private string EntityTestInitializer() =>
        string.Join(", ", ctx.Properties.Select(p => $"{p.Name} = {DbPropertyTypes.GetTestValue(p)}"));

    private string CreateTestArgs() =>
        string.Join(", ", ctx.Properties.Select(p => DbPropertyTypes.GetTestValue(p)));

    private string CreateTestArgsOverride(string overridePropName, string overrideValue) =>
        string.Join(", ", ctx.Properties.Select(p =>
            p.Name == overridePropName ? overrideValue : DbPropertyTypes.GetTestValue(p)));

    private string IdAndPropertiesTestArgs() =>
        ctx.Properties.Count == 0 ? "1" : $"1, {CreateTestArgs()}";

    private string HandlerTestAssertions(string resultVar) =>
        string.Join($"\n{I8}", ctx.Properties
            .Where(DbPropertyTypes.HasStableTestValue)
            .Select(p => $"Assert.Equal({DbPropertyTypes.GetTestValue(p)}, {resultVar}.{p.Name});"));

    private string BuildCreateValidatorTestMethods()
    {
        var sb = new StringBuilder();
        sb.Append($"[Fact]");
        sb.Append($"\n{I4}public void Validate_IsValid_WhenAllPropertiesAreProvided()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.TestValidate(new Create{ctx.Entity}Command({CreateTestArgs()}));");
        sb.Append($"\n{I8}result.ShouldNotHaveAnyValidationErrors();");
        sb.Append($"\n{I4}}}");

        foreach (var p in ctx.Properties.Where(p => p.IsStringType && p.IsRequired))
        {
            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}IsEmpty()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.TestValidate(new Create{ctx.Entity}Command({CreateTestArgsOverride(p.Name, "\"\"")}));");
            sb.Append($"\n{I8}result.ShouldHaveValidationErrorFor(x => x.{p.Name});");
            sb.Append($"\n{I4}}}");

            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}Exceeds255Characters()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.TestValidate(new Create{ctx.Entity}Command({CreateTestArgsOverride(p.Name, "new string('a', 256)")}));");
            sb.Append($"\n{I8}result.ShouldHaveValidationErrorFor(x => x.{p.Name});");
            sb.Append($"\n{I4}}}");
        }

        return sb.ToString();
    }

    private string BuildUpdateValidatorTestMethods()
    {
        var sb = new StringBuilder();
        sb.Append($"[Fact]");
        sb.Append($"\n{I4}public void Validate_IsValid_WhenIdAndPropertiesAreValid()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.TestValidate(new Update{ctx.Entity}Command({IdAndPropertiesTestArgs()}));");
        sb.Append($"\n{I8}result.ShouldNotHaveAnyValidationErrors();");
        sb.Append($"\n{I4}}}");

        sb.Append($"\n\n{I4}[Fact]");
        sb.Append($"\n{I4}public void Validate_IsInvalid_WhenIdIsZero()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.TestValidate(new Update{ctx.Entity}Command(0, {CreateTestArgs()}));");
        sb.Append($"\n{I8}result.ShouldHaveValidationErrorFor(x => x.Id);");
        sb.Append($"\n{I4}}}");

        foreach (var p in ctx.Properties.Where(p => p.IsStringType))
        {
            sb.Append($"\n\n{I4}[Fact]");
            sb.Append($"\n{I4}public void Validate_IsInvalid_When{p.Name}Exceeds255Characters()");
            sb.Append($"\n{I4}{{");
            sb.Append($"\n{I8}var result = _validator.TestValidate(new Update{ctx.Entity}Command(1, {CreateTestArgsOverride(p.Name, "new string('a', 256)")}));");
            sb.Append($"\n{I8}result.ShouldHaveValidationErrorFor(x => x.{p.Name});");
            sb.Append($"\n{I4}}}");
        }

        return sb.ToString();
    }

    // ── Domain ────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> DomainFiles()
    {
        yield return (Path.Combine(ctx.DomainPath, "Entities", $"{ctx.Entity}.cs"), EntityTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", "Repositories", $"I{ctx.Entity}Repository.cs"), IRepositoryTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", Services, $"I{ctx.Entity}DomainService.cs"), IDomainServiceTemplate());
        yield return (Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.cs"), DomainServiceTemplate());
    }

    private string EntityTemplate() => $$"""
        using {{ctx.NS}}.Domain.Interfaces.Repositories;

        namespace {{ctx.NS}}.Domain.Entities;

        public sealed class {{ctx.Entity}} : IEntityOrQueryResult
        {
            public int Id { get; init; }
            {{EntityPropertyDeclarations()}}
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
                Expression<Func<{{ctx.Entity}}, bool>>? query = null;{{DomainServiceFilterBody()}}

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
        var dtoReq = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Requests);
        var dtoRes = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Responses);
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
        yield return (Path.Combine(ctx.AppPath, "Interfaces", Services, $"I{ctx.Entity}ApplicationService.cs"), IApplicationServiceTemplate());
        yield return (Path.Combine(ctx.AppPath, "Mappers", $"{ctx.Entity}MapperProfile.cs"), MapperProfileTemplate());
        yield return (Path.Combine(ctx.AppPath, Services, $"{ctx.Entity}ApplicationService.cs"), ApplicationServiceTemplate());
    }

    private string DtoTemplate(string subNs, string typeName, string body) => $$"""
        namespace {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.{{subNs}};

        public sealed record {{typeName}}({{body}});
        """;

    private string CreateRequestTemplate() => DtoTemplate(Requests, $"Create{ctx.Entity}Request", CreateParams());
    private string UpdateRequestTemplate() => DtoTemplate(Requests, $"Update{ctx.Entity}Request", IdAndPropertiesParams());
    private string DeleteRequestTemplate() => DtoTemplate(Requests, $"Delete{ctx.Entity}Request", "int Id");
    private string FindByIdRequestTemplate() => DtoTemplate(Requests, $"Find{ctx.Entity}ByIdRequest", "int Id");
    private string GetRequestTemplate() => DtoTemplate(Requests, $"Get{ctx.Entity}Request", "string Name = \"\", int Page = 1, int PageSize = 5");
    private string ResponseTemplate() => DtoTemplate(Responses, $"{ctx.Entity}Response", IdAndPropertiesParams());
    private string CreateResponseTemplate() => DtoTemplate(Responses, $"Create{ctx.Entity}Response", IdAndPropertiesParams());
    private string UpdateResponseTemplate() => DtoTemplate(Responses, $"Update{ctx.Entity}Response", IdAndPropertiesParams());
    private string DeleteResponseTemplate() => DtoTemplate(Responses, $"Delete{ctx.Entity}Response", "bool Success");

    private string CommandTemplate(string feature, string name, string parameters, string response) => $$"""
        using MediatR;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;

        namespace {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.{{feature}};

        public sealed record {{name}}({{parameters}}) : IRequest<{{response}}?>;
        """;

    private string CreateCommandTemplate() => CommandTemplate(
        $"Create{ctx.Entity}Feature", $"Create{ctx.Entity}Command", CreateParams(), $"Create{ctx.Entity}Response");

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
        $"Create{ctx.Entity}Feature",
        $"Create{ctx.Entity}Command",
        CreateValidatorRules());

    private string DeleteCommandTemplate() => CommandTemplate(
        $"Delete{ctx.Entity}Feature", $"Delete{ctx.Entity}Command", "int Id", $"Delete{ctx.Entity}Response");

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

    private string DeleteCommandValidatorTemplate() => ValidatorTemplate(
        $"Delete{ctx.Entity}Feature",
        $"Delete{ctx.Entity}Command",
        $"RuleFor(x => x.Id)\n{I12}.NotEmpty()\n{I12}.NotNull();");

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

    private string FindByIdQueryValidatorTemplate() => ValidatorTemplate(
        $"Find{ctx.Entity}ByIdFeature",
        $"Find{ctx.Entity}ByIdQuery",
        "RuleFor(x => x.Id).NotEmpty();");

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

    private string GetQueryValidatorTemplate() => ValidatorTemplate(
        $"Get{ctx.EPlural}Feature",
        $"Get{ctx.Entity}Query",
        $"RuleFor(x => x.Page)\n{I12}.GreaterThanOrEqualTo(1)\n{I12}.WithMessage(\"O número da página deve ser maior ou igual a 1.\");\n\n{I8}RuleFor(x => x.PageSize)\n{I12}.GreaterThanOrEqualTo(5)\n{I12}.WithMessage(\"O tamanho da página deve ser maior ou igual a 5.\");");

    private string UpdateCommandTemplate() => CommandTemplate(
        $"Update{ctx.Entity}Feature", $"Update{ctx.Entity}Command", IdAndPropertiesParams(), $"Update{ctx.Entity}Response");

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

    private string UpdateCommandValidatorTemplate() => ValidatorTemplate(
        $"Update{ctx.Entity}Feature",
        $"Update{ctx.Entity}Command",
        UpdateValidatorRules());

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

                builder.Property(x => x.Id).HasColumnName("Id");{{EfPropertyBlocks()}}
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

    // ── Tests ─────────────────────────────────────────────────────────────────

    private IEnumerable<(string, string)> TestFiles()
    {
        var domainTests = Path.Combine(ctx.TestsPath, "Domain", Services);
        var featTests = Path.Combine(ctx.TestsPath, "Application", "Features", $"{ctx.Entity}Features");
        var appSvcTests = Path.Combine(ctx.TestsPath, "Application", Services);

        yield return (Path.Combine(ctx.AppPath, "Properties", "AssemblyInfo.cs"), AssemblyInfoTemplate());
        yield return (Path.Combine(domainTests, $"{ctx.Entity}DomainServiceTests.cs"), DomainServiceTestsTemplate());
        yield return (Path.Combine(featTests, $"Create{ctx.Entity}CommandHandlerTests.cs"), CreateCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Delete{ctx.Entity}CommandHandlerTests.cs"), DeleteCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Update{ctx.Entity}CommandHandlerTests.cs"), UpdateCommandHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Find{ctx.Entity}ByIdQueryHandlerTests.cs"), FindByIdQueryHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Get{ctx.Entity}QueryHandlerTests.cs"), GetQueryHandlerTestsTemplate());
        yield return (Path.Combine(featTests, $"Create{ctx.Entity}CommandValidatorTests.cs"), CreateCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Delete{ctx.Entity}CommandValidatorTests.cs"), DeleteCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Update{ctx.Entity}CommandValidatorTests.cs"), UpdateCommandValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Find{ctx.Entity}ByIdQueryValidatorTests.cs"), FindByIdQueryValidatorTestsTemplate());
        yield return (Path.Combine(featTests, $"Get{ctx.Entity}QueryValidatorTests.cs"), GetQueryValidatorTestsTemplate());
        yield return (Path.Combine(appSvcTests, $"{ctx.Entity}ApplicationServiceTests.cs"), ApplicationServiceTestsTemplate());
    }

    private string AssemblyInfoTemplate() => $$"""
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("{{ctx.NS}}.Tests.Unit")]
        """;

    private string DomainServiceTestsTemplate() => $$"""
        using System.Linq.Expressions;
        using NSubstitute;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Repositories;
        using {{ctx.NS}}.Domain.Services;

        namespace {{ctx.NS}}.Tests.Unit.Domain.Services;

        public sealed class {{ctx.Entity}}DomainServiceTests
        {
            private readonly I{{ctx.Entity}}Repository _{{ctx.ECamel}}Repository = Substitute.For<I{{ctx.Entity}}Repository>();
            private readonly {{ctx.Entity}}DomainService _service;

            public {{ctx.Entity}}DomainServiceTests()
            {
                _service = new {{ctx.Entity}}DomainService(_{{ctx.ECamel}}Repository);
            }

            [Fact]
            public async Task FindByNamePagedAsync_ReturnsResult_WhenNameIsProvided()
            {
                var entities = new List<{{ctx.Entity}}> { new() { Id = 1, {{EntityTestInitializer()}} } };
                _{{ctx.ECamel}}Repository
                    .CountAsync(Arg.Any<CancellationToken>(), Arg.Any<Expression<Func<{{ctx.Entity}}, bool>>?>())
                    .Returns(1);
                _{{ctx.ECamel}}Repository
                    .FindAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>(),
                        Arg.Any<Expression<Func<{{ctx.Entity}}, bool>>?>(), Arg.Any<int>(), Arg.Any<int>())
                    .Returns(entities);

                var result = await _service.FindByNamePagedAsync("Test", 1, 5, CancellationToken.None);

                Assert.NotNull(result);
            }

            [Fact]
            public async Task FindByNamePagedAsync_ReturnsResult_WhenNameIsEmpty()
            {
                var entities = new List<{{ctx.Entity}}> { new() { Id = 1, {{EntityTestInitializer()}} } };
                _{{ctx.ECamel}}Repository
                    .CountAsync(Arg.Any<CancellationToken>(), Arg.Any<Expression<Func<{{ctx.Entity}}, bool>>?>())
                    .Returns(1);
                _{{ctx.ECamel}}Repository
                    .FindAsync(Arg.Any<CancellationToken>(), Arg.Any<bool>(),
                        Arg.Any<Expression<Func<{{ctx.Entity}}, bool>>?>(), Arg.Any<int>(), Arg.Any<int>())
                    .Returns(entities);

                var result = await _service.FindByNamePagedAsync(string.Empty, 1, 5, CancellationToken.None);

                Assert.NotNull(result);
            }
        }
        """;

    private string CreateCommandHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using NSubstitute;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Create{{ctx.Entity}}CommandHandlerTests
        {
            private readonly I{{ctx.Entity}}DomainService _{{ctx.ECamel}}DomainService = Substitute.For<I{{ctx.Entity}}DomainService>();
            private readonly IMapper _mapper = Substitute.For<IMapper>();
            private readonly Create{{ctx.Entity}}CommandHandler _handler;

            public Create{{ctx.Entity}}CommandHandlerTests()
            {
                _handler = new Create{{ctx.Entity}}CommandHandler(_{{ctx.ECamel}}DomainService, _mapper);
            }

            [Fact]
            public async Task Handle_ReturnsResponse_WhenEntityIsCreated()
            {
                var command = new Create{{ctx.Entity}}Command({{CreateTestArgs()}});
                var entity = new {{ctx.Entity}} { Id = 1, {{EntityTestInitializer()}} };
                var response = new Create{{ctx.Entity}}Response({{IdAndPropertiesTestArgs()}});

                _mapper.Map<{{ctx.Entity}}>(command).Returns(entity);
                _{{ctx.ECamel}}DomainService.AddAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
                _mapper.Map<Create{{ctx.Entity}}Response>(entity).Returns(response);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(1, result.Id);
                {{HandlerTestAssertions("result")}}
            }
        }
        """;

    private string DeleteCommandHandlerTestsTemplate() => $$"""
        using NSubstitute;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Delete{{ctx.Entity}}CommandHandlerTests
        {
            private readonly I{{ctx.Entity}}DomainService _{{ctx.ECamel}}DomainService = Substitute.For<I{{ctx.Entity}}DomainService>();
            private readonly Delete{{ctx.Entity}}CommandHandler _handler;

            public Delete{{ctx.Entity}}CommandHandlerTests()
            {
                _handler = new Delete{{ctx.Entity}}CommandHandler(_{{ctx.ECamel}}DomainService);
            }

            [Fact]
            public async Task Handle_ReturnsSuccess_WhenEntityIsDeleted()
            {
                var command = new Delete{{ctx.Entity}}Command(1);
                _{{ctx.ECamel}}DomainService.RemoveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(true);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.NotNull(result);
                Assert.True(result.Success);
            }

            [Fact]
            public async Task Handle_ReturnsFailure_WhenEntityNotFound()
            {
                var command = new Delete{{ctx.Entity}}Command(999);
                _{{ctx.ECamel}}DomainService.RemoveByIdAsync(999, Arg.Any<CancellationToken>()).Returns(false);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.NotNull(result);
                Assert.False(result.Success);
            }
        }
        """;

    private string UpdateCommandHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using NSubstitute;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Update{{ctx.Entity}}CommandHandlerTests
        {
            private readonly I{{ctx.Entity}}DomainService _{{ctx.ECamel}}DomainService = Substitute.For<I{{ctx.Entity}}DomainService>();
            private readonly IMapper _mapper = Substitute.For<IMapper>();
            private readonly Update{{ctx.Entity}}CommandHandler _handler;

            public Update{{ctx.Entity}}CommandHandlerTests()
            {
                _handler = new Update{{ctx.Entity}}CommandHandler(_{{ctx.ECamel}}DomainService, _mapper);
            }

            [Fact]
            public async Task Handle_ReturnsResponse_WhenEntityIsUpdated()
            {
                var command = new Update{{ctx.Entity}}Command({{IdAndPropertiesTestArgs()}});
                var entity = new {{ctx.Entity}} { Id = 1, {{EntityTestInitializer()}} };
                var response = new Update{{ctx.Entity}}Response({{IdAndPropertiesTestArgs()}});

                _mapper.Map<{{ctx.Entity}}>(command).Returns(entity);
                _{{ctx.ECamel}}DomainService.UpdateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
                _mapper.Map<Update{{ctx.Entity}}Response>(entity).Returns(response);

                var result = await _handler.Handle(command, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(1, result.Id);
                {{HandlerTestAssertions("result")}}
            }
        }
        """;

    private string FindByIdQueryHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using NSubstitute;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Domain.Entities;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Find{{ctx.Entity}}ByIdQueryHandlerTests
        {
            private readonly I{{ctx.Entity}}DomainService _{{ctx.ECamel}}DomainService = Substitute.For<I{{ctx.Entity}}DomainService>();
            private readonly IMapper _mapper = Substitute.For<IMapper>();
            private readonly Find{{ctx.Entity}}ByIdQueryHandler _handler;

            public Find{{ctx.Entity}}ByIdQueryHandlerTests()
            {
                _handler = new Find{{ctx.Entity}}ByIdQueryHandler(_{{ctx.ECamel}}DomainService, _mapper);
            }

            [Fact]
            public async Task Handle_ReturnsResponse_WhenEntityIsFound()
            {
                var query = new Find{{ctx.Entity}}ByIdQuery(1);
                var entity = new {{ctx.Entity}} { Id = 1, {{EntityTestInitializer()}} };
                var response = new {{ctx.Entity}}Response({{IdAndPropertiesTestArgs()}});

                _{{ctx.ECamel}}DomainService.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
                _mapper.Map<{{ctx.Entity}}Response>(entity).Returns(response);

                var result = await _handler.Handle(query, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(1, result.Id);
            }
        }
        """;

    private string GetQueryHandlerTestsTemplate() => $$"""
        using AutoMapper;
        using NSubstitute;
        using {{ctx.NS}}.Application.DTOs.Base.Response;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Domain.Interfaces.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Get{{ctx.Entity}}QueryHandlerTests
        {
            private readonly I{{ctx.Entity}}DomainService _{{ctx.ECamel}}DomainService = Substitute.For<I{{ctx.Entity}}DomainService>();
            private readonly IMapper _mapper = Substitute.For<IMapper>();
            private readonly Get{{ctx.Entity}}QueryHandler _handler;

            public Get{{ctx.Entity}}QueryHandlerTests()
            {
                _handler = new Get{{ctx.Entity}}QueryHandler(_{{ctx.ECamel}}DomainService, _mapper);
            }

            [Fact]
            public async Task Handle_CallsServiceWithCorrectParameters()
            {
                var query = new Get{{ctx.Entity}}Query("Search", 2, 10);

                await _handler.Handle(query, CancellationToken.None);

                await _{{ctx.ECamel}}DomainService
                    .Received(1)
                    .FindByNamePagedAsync("Search", 2, 10, Arg.Any<CancellationToken>());
            }
        }
        """;

    private string CreateCommandValidatorTestsTemplate() => $$"""
        using FluentValidation.TestHelper;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Create{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Create{{ctx.Entity}}CommandValidator _validator = new();

            {{BuildCreateValidatorTestMethods()}}
        }
        """;

    private string DeleteCommandValidatorTestsTemplate() => $$"""
        using FluentValidation.TestHelper;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Delete{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Delete{{ctx.Entity}}CommandValidator _validator = new();

            [Fact]
            public void Validate_IsValid_WhenIdIsProvided()
            {
                var result = _validator.TestValidate(new Delete{{ctx.Entity}}Command(1));
                result.ShouldNotHaveAnyValidationErrors();
            }

            [Fact]
            public void Validate_IsInvalid_WhenIdIsZero()
            {
                var result = _validator.TestValidate(new Delete{{ctx.Entity}}Command(0));
                result.ShouldHaveValidationErrorFor(x => x.Id);
            }
        }
        """;

    private string UpdateCommandValidatorTestsTemplate() => $$"""
        using FluentValidation.TestHelper;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Update{{ctx.Entity}}CommandValidatorTests
        {
            private readonly Update{{ctx.Entity}}CommandValidator _validator = new();

            {{BuildUpdateValidatorTestMethods()}}
        }
        """;

    private string FindByIdQueryValidatorTestsTemplate() => $$"""
        using FluentValidation.TestHelper;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Find{{ctx.Entity}}ByIdQueryValidatorTests
        {
            private readonly Find{{ctx.Entity}}ByIdQueryValidator _validator = new();

            [Fact]
            public void Validate_IsValid_WhenIdIsProvided()
            {
                var result = _validator.TestValidate(new Find{{ctx.Entity}}ByIdQuery(1));
                result.ShouldNotHaveAnyValidationErrors();
            }

            [Fact]
            public void Validate_IsInvalid_WhenIdIsZero()
            {
                var result = _validator.TestValidate(new Find{{ctx.Entity}}ByIdQuery(0));
                result.ShouldHaveValidationErrorFor(x => x.Id);
            }
        }
        """;

    private string GetQueryValidatorTestsTemplate() => $$"""
        using FluentValidation.TestHelper;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;

        namespace {{ctx.NS}}.Tests.Unit.Application.Features.{{ctx.Entity}}Features;

        public sealed class Get{{ctx.Entity}}QueryValidatorTests
        {
            private readonly Get{{ctx.Entity}}QueryValidator _validator = new();

            [Fact]
            public void Validate_IsValid_WhenPageAndPageSizeAreValid()
            {
                var result = _validator.TestValidate(new Get{{ctx.Entity}}Query("", 1, 5));
                result.ShouldNotHaveAnyValidationErrors();
            }

            [Fact]
            public void Validate_IsInvalid_WhenPageIsZero()
            {
                var result = _validator.TestValidate(new Get{{ctx.Entity}}Query("", 0, 5));
                result.ShouldHaveValidationErrorFor(x => x.Page);
            }

            [Fact]
            public void Validate_IsInvalid_WhenPageSizeIsBelowMinimum()
            {
                var result = _validator.TestValidate(new Get{{ctx.Entity}}Query("", 1, 4));
                result.ShouldHaveValidationErrorFor(x => x.PageSize);
            }
        }
        """;

    private string ApplicationServiceTestsTemplate() => $$"""
        using AutoMapper;
        using MediatR;
        using NSubstitute;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Requests;
        using {{ctx.NS}}.Application.DTOs.{{ctx.Entity}}.Responses;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Create{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Delete{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Find{{ctx.Entity}}ByIdFeature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Get{{ctx.EPlural}}Feature;
        using {{ctx.NS}}.Application.Features.{{ctx.Entity}}Features.Update{{ctx.Entity}}Feature;
        using {{ctx.NS}}.Application.Services;

        namespace {{ctx.NS}}.Tests.Unit.Application.Services;

        public sealed class {{ctx.Entity}}ApplicationServiceTests
        {
            private readonly IMediator _mediator = Substitute.For<IMediator>();
            private readonly IMapper _mapper = Substitute.For<IMapper>();
            private readonly {{ctx.Entity}}ApplicationService _service;

            public {{ctx.Entity}}ApplicationServiceTests()
            {
                _service = new {{ctx.Entity}}ApplicationService(_mediator, _mapper);
            }

            [Fact]
            public async Task CreateAsync_SendsCommand_ToMediator()
            {
                var request = new Create{{ctx.Entity}}Request({{CreateTestArgs()}});
                var command = new Create{{ctx.Entity}}Command({{CreateTestArgs()}});
                _mapper.Map<Create{{ctx.Entity}}Command>(request).Returns(command);

                await _service.CreateAsync(request, CancellationToken.None);

                await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task UpdateAsync_SendsCommand_ToMediator()
            {
                var request = new Update{{ctx.Entity}}Request({{IdAndPropertiesTestArgs()}});
                var command = new Update{{ctx.Entity}}Command({{IdAndPropertiesTestArgs()}});
                _mapper.Map<Update{{ctx.Entity}}Command>(request).Returns(command);

                await _service.UpdateAsync(request, CancellationToken.None);

                await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task DeleteAsync_SendsCommand_ToMediator()
            {
                var request = new Delete{{ctx.Entity}}Request(1);
                var command = new Delete{{ctx.Entity}}Command(1);
                _mapper.Map<Delete{{ctx.Entity}}Command>(request).Returns(command);

                await _service.DeleteAsync(request, CancellationToken.None);

                await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task GetByIdAsync_SendsQuery_ToMediator()
            {
                var request = new Find{{ctx.Entity}}ByIdRequest(1);
                var query = new Find{{ctx.Entity}}ByIdQuery(1);
                _mapper.Map<Find{{ctx.Entity}}ByIdQuery>(request).Returns(query);

                await _service.GetByIdAsync(request, CancellationToken.None);

                await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task GetAsync_SendsQuery_ToMediator()
            {
                var request = new Get{{ctx.Entity}}Request("", 1, 5);
                var query = new Get{{ctx.Entity}}Query("", 1, 5);
                _mapper.Map<Get{{ctx.Entity}}Query>(request).Returns(query);

                await _service.GetAsync(request, CancellationToken.None);

                await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
            }
        }
        """;

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
