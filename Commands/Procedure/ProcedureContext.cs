using OpenBase.CLI.Models;

namespace OpenBase.CLI.Commands.Procedure;

public sealed record ProcedureContext(string ProcedureName, string RootNamespace, string SolutionDir)
{
    public string PCamel => char.ToLowerInvariant(ProcedureName[0]) + ProcedureName[1..];
    public string NS => RootNamespace;

    public IReadOnlyList<ProcedureParameter> Parameters { get; init; } = [];

    public IReadOnlyList<ProcedureParameter> InputParams  => [.. Parameters.Where(p => p.IsInput)];
    public IReadOnlyList<ProcedureParameter> OutputParams => [.. Parameters.Where(p => p.IsOutput)];

    private string Src => Path.Combine(SolutionDir, "src");
    public string AppPath      => Path.Combine(Src, $"{NS}.Application");
    public string DomainPath   => Path.Combine(Src, $"{NS}.Domain");
    public string InfraDataPath => Path.Combine(Src, $"{NS}.Infra.Data");

    private string? _testsPathOverride;
    public string TestsPath
    {
        get => _testsPathOverride ?? Path.Combine(SolutionDir, "tests", $"{NS}.Tests.Unit");
        init => _testsPathOverride = value;
    }
    public string TestsCsprojPath => Path.Combine(TestsPath, $"{NS}.Tests.Unit.csproj");
}
