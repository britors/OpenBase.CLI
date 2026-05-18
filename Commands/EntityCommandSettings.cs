using System.ComponentModel;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public abstract class EntityCommandSettings : CommandSettings
{
    [CommandOption("-e|--entity <ENTIDADE>")]
    [Description("O nome da entidade (PascalCase, ex: Produto)")]
    public string Entity { get; set; } = string.Empty;

    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Namespace raiz do projeto (detectado automaticamente se omitido)")]
    public string? RootNamespace { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Entity))
            return ValidationResult.Error(SR.Current.EntityParamRequired);

        if (!char.IsUpper(Entity[0]))
            return ValidationResult.Error(SR.Current.EntityMustBePascalCase);

        if (!Entity.All(char.IsLetterOrDigit))
            return ValidationResult.Error(SR.Current.EntityMustBeAlphanumeric);

        return ValidationResult.Success();
    }
}
