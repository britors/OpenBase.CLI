namespace OpenBase.CLI.Models;

public enum ParameterDirection { In, Out, InOut }

public sealed record ProcedureParameter(string Name, string CsType, ParameterDirection Direction)
{
    public string CamelName => char.ToLowerInvariant(Name[0]) + Name[1..];
    public bool IsInput  => Direction is ParameterDirection.In  or ParameterDirection.InOut;
    public bool IsOutput => Direction is ParameterDirection.Out or ParameterDirection.InOut;
}
