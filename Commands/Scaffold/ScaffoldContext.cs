using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Scaffold;

public sealed record ScaffoldContext(string Entity, string RootNamespace, string SolutionDir)
{
    public string ECamel => char.ToLowerInvariant(Entity[0]) + Entity[1..];
    public string EPlural => Entity + "s";
    public string ELower => Entity.ToLowerInvariant();
    public string NS => RootNamespace;

    public IReadOnlyList<EntityProperty> Properties { get; init; } = DefaultProperties;
    public DbFlavor DbFlavor { get; init; } = DbFlavor.SqlServer;
    public string? TableName { get; init; }
    public string? Schema { get; init; }
    public bool UseJwt { get; init; } = false;

    public EntityProperty? FilterProperty =>
        Properties.FirstOrDefault(p => p.IsStringType && p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
        ?? Properties.FirstOrDefault(p => p.IsStringType);

    private string Src => Path.Combine(SolutionDir, "src");
    public string DomainPath => Path.Combine(Src, $"{NS}.Domain");
    public string AppPath => Path.Combine(Src, $"{NS}.Application");
    public string InfraContextPath => Path.Combine(Src, $"{NS}.Infra.Data.Context");
    public string InfraDataPath => Path.Combine(Src, $"{NS}.Infra.Data");
    public string PresentationPath => Path.Combine(Src, $"{NS}.Presentation.Api");
    private string? _testsPathOverride;
    public string TestsPath
    {
        get => _testsPathOverride ?? Path.Combine(SolutionDir, "tests", $"{NS}.Tests.Unit");
        init => _testsPathOverride = value;
    }
    public string TestsCsprojPath => Path.Combine(TestsPath, $"{NS}.Tests.Unit.csproj");

    private static readonly IReadOnlyList<EntityProperty> DefaultProperties =
        [new EntityProperty("Name", "string", true)];
}
