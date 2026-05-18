using System.Text;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Scaffold;

public sealed partial class ScaffoldGenerator(ScaffoldContext ctx)
{
    private const string Services     = "Services";
    private const string Interfaces   = "Interfaces";
    private const string Repositories = "Repositories";
    private const string Requests   = "Requests";
    private const string Responses  = "Responses";
    private const string IntId      = "int Id";

    private const string I4  = "    ";
    private const string I8  = "        ";
    private const string I12 = "            ";

    public IEnumerable<(string Path, string Content)> GetFiles() =>
        DomainFiles()
            .Concat(ApplicationFiles())
            .Concat(InfrastructureFiles())
            .Concat(PresentationFiles())
            .Concat(TestFiles());

    public IEnumerable<(string Path, string Content)> GetPropertyDependentFiles()
    {
        // Domain
        yield return (Path.Combine(ctx.DomainPath, "Entities", $"{ctx.Entity}.cs"), EntityTemplate());
        yield return (Path.Combine(ctx.DomainPath, "Interfaces", Services, $"I{ctx.Entity}DomainService.cs"), IDomainServiceTemplate());
        yield return (Path.Combine(ctx.DomainPath, Services, $"{ctx.Entity}DomainService.cs"), DomainServiceTemplate());

        // Application — DTOs
        var dtoReq = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Requests);
        var dtoRes = Path.Combine(ctx.AppPath, "DTOs", ctx.Entity, Responses);
        yield return (Path.Combine(dtoReq, $"Create{ctx.Entity}Request.cs"), CreateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Update{ctx.Entity}Request.cs"), UpdateRequestTemplate());
        yield return (Path.Combine(dtoReq, $"Get{ctx.Entity}Request.cs"), GetRequestTemplate());
        yield return (Path.Combine(dtoRes, $"{ctx.Entity}Response.cs"), ResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Create{ctx.Entity}Response.cs"), CreateResponseTemplate());
        yield return (Path.Combine(dtoRes, $"Update{ctx.Entity}Response.cs"), UpdateResponseTemplate());

        // Application — Features (property-dependent)
        var feat   = Path.Combine(ctx.AppPath, "Features", $"{ctx.Entity}Features");
        var create = Path.Combine(feat, $"Create{ctx.Entity}Feature");
        yield return (Path.Combine(create, $"Create{ctx.Entity}Command.cs"), CreateCommandTemplate());
        yield return (Path.Combine(create, $"Create{ctx.Entity}CommandValidator.cs"), CreateCommandValidatorTemplate());

        var get = Path.Combine(feat, $"Get{ctx.EPlural}Feature");
        yield return (Path.Combine(get, $"Get{ctx.Entity}Query.cs"), GetQueryTemplate());
        yield return (Path.Combine(get, $"Get{ctx.Entity}QueryHandler.cs"), GetQueryHandlerTemplate());

        var update = Path.Combine(feat, $"Update{ctx.Entity}Feature");
        yield return (Path.Combine(update, $"Update{ctx.Entity}Command.cs"), UpdateCommandTemplate());
        yield return (Path.Combine(update, $"Update{ctx.Entity}CommandValidator.cs"), UpdateCommandValidatorTemplate());

        // Infrastructure
        yield return (
            Path.Combine(ctx.InfraContextPath, "Configurations", $"{ctx.Entity}Configuration.cs"),
            EfConfigurationTemplate());
    }


    private string EntityPropertyDeclarations() =>
        string.Join($"\n{I4}", ctx.Properties.Select(PropertyDeclaration));

    private static string PropertyDeclaration(EntityProperty p) =>
        p.IsStringType && p.IsRequired
            ? $"public {p.ActualCsType} {p.Name} {{ get; init; }} = string.Empty;"
            : $"public {p.ActualCsType} {p.Name} {{ get; init; }}";

    private string CreateParams() =>
        string.Join(", ", ctx.Properties.Select(p => $"{p.ActualCsType} {p.Name}"));

    private string IdAndPropertiesParams() =>
        ctx.Properties.Count == 0 ? IntId : $"{IntId}, {CreateParams()}";

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


    private static string ToCamel(string s)  => char.ToLowerInvariant(s[0]) + s[1..];
    private static string ToPascal(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    private IEnumerable<EntityProperty> FilterableProperties =>
        ctx.Properties.Where(p => p.CsType is not ("byte[]" or "JsonDocument"));

    private string FilterParamsWithDefaults() =>
        string.Join(", ", FilterableProperties.Select(p =>
            p.IsStringType ? $"string? {ToPascal(p.Name)} = null"
                           : $"{p.CsType}? {ToPascal(p.Name)} = null"));

    private string FilterParamsNoDefaults() =>
        string.Join(", ", FilterableProperties.Select(p =>
            p.IsStringType ? $"string? {ToPascal(p.Name)}"
                           : $"{p.CsType}? {ToPascal(p.Name)}"));

    private string FilterNullArgs() =>
        string.Join(", ", FilterableProperties.Select(_ => "null"));

    private string FilterNullArgsWithComma =>
        FilterableProperties.Any() ? FilterNullArgs() + ", " : "";

    private string FindByArgumentsCallArgs()
    {
        var parts = FilterableProperties.Select(p => $"request.{p.Name}").ToList();
        parts.Add("request.Page");
        parts.Add("request.PageSize");
        parts.Add("cancellationToken");
        return string.Join(", ", parts);
    }

    private string FindByArgumentsSignatureParams()
    {
        var parts = FilterableProperties.Select(p =>
            p.IsStringType ? $"string? {ToCamel(p.Name)} = null"
                           : $"{p.CsType}? {ToCamel(p.Name)} = null").ToList();
        parts.Add("int page = 1");
        parts.Add("int pageSize = 5");
        parts.Add("CancellationToken cancellationToken = default");
        return string.Join(", ", parts);
    }

    private string FilterBodyCode()
    {
        var sb = new StringBuilder();
        sb.Append($"Expression<Func<{ctx.Entity}, bool>>? filter = null;");
        foreach (var p in FilterableProperties)
        {
            var param = ToCamel(p.Name);
            var check = p.IsStringType ? $"!string.IsNullOrWhiteSpace({param})" : $"{param}.HasValue";
            var cond  = p.IsStringType ? $"x.{p.Name}.Contains({param}!)" : $"x.{p.Name} == {param}.Value";
            sb.Append($"\n{I8}if ({check}) filter = And(filter, x => {cond});");
        }
        return sb.ToString();
    }


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

    private string FindByArgumentsNullTestArgs(int page = 1, int pageSize = 5)
    {
        var parts = FilterableProperties.Select(_ => "null").ToList();
        parts.Add(page.ToString());
        parts.Add(pageSize.ToString());
        parts.Add("CancellationToken.None");
        return string.Join(", ", parts);
    }

    private string FindByArgumentsOneFilterTestArgs(int page = 1, int pageSize = 5)
    {
        var stringProp = FilterableProperties.FirstOrDefault(p => p.IsStringType);
        var parts = FilterableProperties.Select(p => p == stringProp ? "\"Test\"" : "null").ToList();
        parts.Add(page.ToString());
        parts.Add(pageSize.ToString());
        parts.Add("CancellationToken.None");
        return string.Join(", ", parts);
    }


    private string BuildCreateValidatorTestMethods()
    {
        var sb = new StringBuilder();
        sb.Append("[Fact]");
        sb.Append($"\n{I4}public void Validate_IsValid_WhenAllPropertiesAreProvided()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.Validate(new Create{ctx.Entity}Command({CreateTestArgs()}));");
        sb.Append($"\n{I8}Assert.True(result.IsValid);");
        sb.Append($"\n{I4}}}");

        foreach (var name in ctx.Properties.Where(p => p.IsStringType && p.IsRequired).Select(p => p.Name))
        {
            AppendValidatorFact(sb, $"Validate_IsInvalid_When{name}IsEmpty",
                $"Create{ctx.Entity}Command({CreateTestArgsOverride(name, "\"\"")})", name);
            AppendValidatorFact(sb, $"Validate_IsInvalid_When{name}Exceeds255Characters",
                $"Create{ctx.Entity}Command({CreateTestArgsOverride(name, "new string('a', 256)")})", name);
        }

        return sb.ToString();
    }

    private string BuildUpdateValidatorTestMethods()
    {
        var sb = new StringBuilder();
        sb.Append("[Fact]");
        sb.Append($"\n{I4}public void Validate_IsValid_WhenIdAndPropertiesAreValid()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.Validate(new Update{ctx.Entity}Command({IdAndPropertiesTestArgs()}));");
        sb.Append($"\n{I8}Assert.True(result.IsValid);");
        sb.Append($"\n{I4}}}");

        AppendValidatorFact(sb, "Validate_IsInvalid_WhenIdIsZero",
            $"Update{ctx.Entity}Command(0, {CreateTestArgs()})", "Id");

        foreach (var name in ctx.Properties.Where(p => p.IsStringType).Select(p => p.Name))
        {
            AppendValidatorFact(sb, $"Validate_IsInvalid_When{name}Exceeds255Characters",
                $"Update{ctx.Entity}Command(1, {CreateTestArgsOverride(name, "new string('a', 256)")})", name);
        }

        return sb.ToString();
    }

    private static void AppendValidatorFact(StringBuilder sb, string methodName, string commandCtor, string propName)
    {
        sb.Append($"\n\n{I4}[Fact]");
        sb.Append($"\n{I4}public void {methodName}()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.Validate(new {commandCtor});");
        sb.Append($"\n{I8}Assert.False(result.IsValid);");
        sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"{propName}\");");
        sb.Append($"\n{I4}}}");
    }

    private static string BuildIdValidatorTestMethods(string typeName)
    {
        var sb = new StringBuilder();
        sb.Append("[Fact]");
        sb.Append($"\n{I4}public void Validate_IsValid_WhenIdIsProvided()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.Validate(new {typeName}(1));");
        sb.Append($"\n{I8}Assert.True(result.IsValid);");
        sb.Append($"\n{I4}}}");
        sb.Append($"\n\n{I4}[Fact]");
        sb.Append($"\n{I4}public void Validate_IsInvalid_WhenIdIsZero()");
        sb.Append($"\n{I4}{{");
        sb.Append($"\n{I8}var result = _validator.Validate(new {typeName}(0));");
        sb.Append($"\n{I8}Assert.False(result.IsValid);");
        sb.Append($"\n{I8}Assert.Contains(result.Errors, e => e.PropertyName == \"Id\");");
        sb.Append($"\n{I4}}}");
        return sb.ToString();
    }
}
