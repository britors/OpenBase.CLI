using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

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
        console.MarkupLine(SR.Current.EntityProperties);
        console.MarkupLine(string.Format(SR.Current.DatabaseAndTypes, dbFlavor, Markup.Escape(string.Join(", ", validTypes))));
        console.WriteLine();

        do
        {
            var name = console.Prompt(
                new TextPrompt<string>(string.Format(SR.Current.PropertyNamePrompt, properties.Count + 1))
                    .Validate(n => ValidatePropertyName(n, properties)));

            var type = console.Prompt(
                new SelectionPrompt<string>()
                    .Title(SR.Current.PropertyTypePrompt)
                    .AddChoices(validTypes));

            var isRequired = console.Confirm(SR.Current.PropertyNotNull, defaultValue: true);

            properties.Add(new EntityProperty(name, type, isRequired));
            console.MarkupLine(string.Format(SR.Current.PropertyAdded, name, isRequired ? type : type + "?"));
            console.WriteLine();

        } while (properties.Count == 0 ||
                 console.Confirm(SR.Current.AddAnotherProperty, defaultValue: false));

        ShowSummaryTable(properties);
        return properties;
    }

    private static ValidationResult ValidatePropertyName(string n, List<EntityProperty> properties)
    {
        if (string.IsNullOrWhiteSpace(n))
            return ValidationResult.Error(SR.Current.PropNameRequired);
        if (!char.IsUpper(n[0]))
            return ValidationResult.Error(SR.Current.PropNameMustStartUpper);
        if (!n.All(char.IsLetterOrDigit))
            return ValidationResult.Error(SR.Current.PropNameAlphanumericOnly);
        if (n.Equals(ReservedIdName, StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error(SR.Current.PropNameIdReserved);
        if (properties.Any(p => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Error(string.Format(SR.Current.PropNameAlreadyAdded, n));
        return ValidationResult.Success();
    }

    private void ShowSummaryTable(IReadOnlyList<EntityProperty> properties)
    {
        console.WriteLine();

        var table = new Table()
            .AddColumn(SR.Current.ColProperty)
            .AddColumn(SR.Current.ColType)
            .AddColumn(SR.Current.ColPK)
            .AddColumn(SR.Current.ColNotNull);

        table.AddRow(ReservedIdName, IdType, "[green]✓[/]", "[green]✓[/]");

        foreach (var p in properties)
        {
            table.AddRow(
                p.Name,
                p.CsType,
                "[grey]-[/]",
                p.IsRequired ? "[green]✓[/]" : "[grey]-[/]");
        }

        console.Write(table);
        console.WriteLine();
    }
}
