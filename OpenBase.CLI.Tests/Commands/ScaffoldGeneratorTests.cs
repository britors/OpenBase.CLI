using OpenBase.CLI.Commands;

namespace OpenBase.CLI.Tests.Commands;

public class ScaffoldGeneratorTests
{
    private static ScaffoldContext MakeContext(string entity = "Produto", string ns = "MyApp", string dir = "/sol") =>
        new(entity, ns, dir);

    private static ScaffoldGenerator MakeGenerator(string entity = "Produto") =>
        new(MakeContext(entity));

    [Fact]
    public void GetFiles_Returns34Files()
    {
        var files = MakeGenerator().GetFiles().ToList();
        Assert.Equal(34, files.Count);
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
    public void GetFiles_EntityNameAppearsInAllContents()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        Assert.All(files, f => Assert.Contains("Produto", f.Content));
    }

    [Fact]
    public void GetFiles_NamespaceAppearsInAllContents()
    {
        var ctx = MakeContext(ns: "MinhaEmpresa");
        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();
        Assert.All(files, f => Assert.Contains("MinhaEmpresa", f.Content));
    }

    [Fact]
    public void Context_ECamel_IsLowercaseFirstLetter()
    {
        var ctx = new ScaffoldContext("Produto", "NS", "/");
        Assert.Equal("produto", ctx.ECamel);
    }

    [Fact]
    public void Context_ECamel_PreservesRestOfName()
    {
        var ctx = new ScaffoldContext("MinhaEntidade", "NS", "/");
        Assert.Equal("minhaEntidade", ctx.ECamel);
    }

    [Fact]
    public void Context_EPlural_AppendsS()
    {
        var ctx = new ScaffoldContext("Produto", "NS", "/");
        Assert.Equal("Produtos", ctx.EPlural);
    }

    [Fact]
    public void Context_ELower_IsAllLowercase()
    {
        var ctx = new ScaffoldContext("Produto", "NS", "/");
        Assert.Equal("produto", ctx.ELower);
    }

    [Fact]
    public void GetFiles_DomainEntityFileHasCorrectPath()
    {
        var ctx = MakeContext("Produto", "MyApp", "/sol");
        var files = new ScaffoldGenerator(ctx).GetFiles().ToList();

        var entityFile = files.FirstOrDefault(f => f.Path.EndsWith($"Produto.cs")
            && f.Path.Contains("Entities"));

        Assert.NotNull(entityFile.Path);
        Assert.Contains(Path.Combine("MyApp.Domain", "Entities", "Produto.cs"), entityFile.Path);
    }

    [Fact]
    public void GetFiles_ControllerFileHasRouteLowercase()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var controller = files.First(f => f.Path.EndsWith("ProdutoController.cs"));

        Assert.Contains("api/produto", controller.Content);
    }

    [Fact]
    public void GetFiles_EfConfigurationHasPluralTableName()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var config = files.First(f => f.Path.EndsWith("ProdutoConfiguration.cs"));

        Assert.Contains("\"Produtos\"", config.Content);
    }

    [Fact]
    public void GetFiles_RepositoryInheritsRepositoryBase()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var repo = files.First(f => f.Path.EndsWith("ProdutoRepository.cs"));

        Assert.Contains("RepositoryBase<Produto>", repo.Content);
        Assert.Contains("IProdutoRepository", repo.Content);
    }

    [Fact]
    public void GetFiles_DomainServiceImplementsInterface()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var svc = files.First(f => f.Path.EndsWith("ProdutoDomainService.cs"));

        Assert.Contains("IProdutoDomainService", svc.Content);
        Assert.Contains("FindByNamePagedAsync", svc.Content);
    }

    [Fact]
    public void GetFiles_MapperProfileIncludesAllMappings()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var mapper = files.First(f => f.Path.EndsWith("ProdutoMapperProfile.cs"));

        Assert.Contains("CreateMap<CreateProdutoRequest, CreateProdutoCommand>", mapper.Content);
        Assert.Contains("CreateMap<Produto, ProdutoResponse>", mapper.Content);
        Assert.Contains("CreateMap<PaginatedQueryResult<Produto>, PaginatedResponse<ProdutoResponse>>", mapper.Content);
    }

    // ── ValidatorTemplate ─────────────────────────────────────────────────────

    [Fact]
    public void GetFiles_ExactlyFiveValidatorFiles()
    {
        var validators = MakeGenerator().GetFiles()
            .Where(f => f.Path.EndsWith("Validator.cs")).ToList();

        Assert.Equal(5, validators.Count);
    }

    [Fact]
    public void GetFiles_AllValidators_ContainFluentValidationUsing()
    {
        var validators = MakeGenerator().GetFiles()
            .Where(f => f.Path.EndsWith("Validator.cs"));

        Assert.All(validators, f => Assert.Contains("using FluentValidation;", f.Content));
    }

    [Fact]
    public void GetFiles_AllValidators_InheritAbstractValidator()
    {
        var validators = MakeGenerator().GetFiles()
            .Where(f => f.Path.EndsWith("Validator.cs"));

        Assert.All(validators, f => Assert.Contains("AbstractValidator<", f.Content));
    }

    [Fact]
    public void GetFiles_AllValidators_HaveMatchingNamespaceAndClassName()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var validators = files.Where(f => f.Path.EndsWith("Validator.cs")).ToList();

        Assert.All(validators, f =>
        {
            var className = Path.GetFileNameWithoutExtension(f.Path);
            Assert.Contains(className, f.Content);
        });
    }

    [Fact]
    public void GetFiles_CreateCommandValidator_HasNameRules()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var v = files.First(f => f.Path.EndsWith("CreateProdutoCommandValidator.cs"));

        Assert.Contains("RuleFor(x => x.Name)", v.Content);
        Assert.Contains(".NotEmpty()", v.Content);
        Assert.Contains(".MinimumLength(1)", v.Content);
        Assert.Contains(".MaximumLength(255)", v.Content);
    }

    [Fact]
    public void GetFiles_DeleteCommandValidator_HasIdRules()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var v = files.First(f => f.Path.EndsWith("DeleteProdutoCommandValidator.cs"));

        Assert.Contains("RuleFor(x => x.Id)", v.Content);
        Assert.Contains(".NotEmpty()", v.Content);
        Assert.Contains(".NotNull()", v.Content);
    }

    [Fact]
    public void GetFiles_FindByIdQueryValidator_HasIdNotEmptyRule()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var v = files.First(f => f.Path.EndsWith("FindProdutoByIdQueryValidator.cs"));

        Assert.Contains("RuleFor(x => x.Id).NotEmpty();", v.Content);
    }

    [Fact]
    public void GetFiles_GetQueryValidator_HasPageAndPageSizeRules()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var v = files.First(f => f.Path.EndsWith("GetProdutoQueryValidator.cs"));

        Assert.Contains("RuleFor(x => x.Page)", v.Content);
        Assert.Contains(".GreaterThanOrEqualTo(1)", v.Content);
        Assert.Contains("RuleFor(x => x.PageSize)", v.Content);
        Assert.Contains(".GreaterThanOrEqualTo(5)", v.Content);
    }

    [Fact]
    public void GetFiles_UpdateCommandValidator_HasIdAndNameRules()
    {
        var files = MakeGenerator("Produto").GetFiles().ToList();
        var v = files.First(f => f.Path.EndsWith("UpdateProdutoCommandValidator.cs"));

        Assert.Contains("RuleFor(x => x.Id)", v.Content);
        Assert.Contains("RuleFor(x => x.Name)", v.Content);
        Assert.Contains(".MinimumLength(1)", v.Content);
        Assert.Contains(".MaximumLength(255)", v.Content);
        Assert.Contains("string.IsNullOrWhiteSpace", v.Content);
    }
}
