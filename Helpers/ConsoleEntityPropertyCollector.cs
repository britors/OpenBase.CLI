using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Models;
using Spectre.Console;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class ConsoleEntityPropertyCollector(IAnsiConsole console) : IEntityPropertyCollector
{
    private const string ReservedIdName = "Id";
    private const string IdType = "int";

    public IReadOnlyList<EntityProperty> Collect(DbFlavor dbFlavor)
    {
        var validTypes = DbPropertyTypes.GetValidTypes(dbFlavor);
        var properties = new List<EntityProperty>();

        console.WriteLine();
        console.MarkupLine("[bold]Propriedades da entidade[/]");
        console.MarkupLine($"[grey]Banco: [blue]{dbFlavor}[/] | Tipos disponíveis: {Markup.Escape(string.Join(", ", validTypes))}[/]");
        console.WriteLine();

        do
        {
            var name = console.Prompt(
                new TextPrompt<string>($"[bold]Prop {properties.Count + 1}[/] — Nome [grey](PascalCase)[/]:")
                    .Validate(n => ValidatePropertyName(n, properties)));

            var type = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("  Tipo:")
                    .AddChoices(validTypes));

            var isRequired = console.Confirm("  Not null (obrigatório)?", defaultValue: true);

            properties.Add(new EntityProperty(name, type, isRequired));
            console.MarkupLine($"  [green]+ {name} ({(isRequired ? type : type + "?")})[/]");
            console.WriteLine();

        } while (properties.Count == 0 ||
                 console.Confirm("Adicionar outra propriedade?", defaultValue: false));

        ShowSummaryTable(properties);
        return properties;
    }

    private static ValidationResult ValidatePropertyName(string n, List<EntityProperty> properties)
    {
        if (string.IsNullOrWhiteSpace(n))
            return ValidationResult.Error("O nome é obrigatório.");
        if (!char.IsUpper(n[0]))
            return ValidationResult.Error("Deve começar com letra maiúscula.");
        if (!n.All(char.IsLetterOrDigit))
            return ValidationResult.Error("Use apenas letras e números.");
        if (n.Equals(ReservedIdName, StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error("'Id' é reservado como chave primária.");
        if (properties.Any(p => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Error($"Propriedade '{n}' já foi adicionada.");
        return ValidationResult.Success();
    }

    private void ShowSummaryTable(IReadOnlyList<EntityProperty> properties)
    {
        console.WriteLine();

        var table = new Table()
            .AddColumn("Propriedade")
            .AddColumn("Tipo")
            .AddColumn("PK")
            .AddColumn("Not Null");

        table.AddRow(ReservedIdName, IdType, "[green]Sim[/]", "[green]Sim[/]");

        foreach (var p in properties)
        {
            table.AddRow(
                p.Name,
                p.CsType,
                "[grey]Não[/]",
                p.IsRequired ? "[green]Sim[/]" : "[grey]Não[/]");
        }

        console.Write(table);
        console.WriteLine();
    }
}
