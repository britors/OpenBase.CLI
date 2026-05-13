using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

[ExcludeFromCodeCoverage]
public sealed class ConsoleModelFirstPropertyCollector(
    IAnsiConsole console,
    IDbSchemaReader dbSchemaReader,
    IConnectionStringReader connectionStringReader)
    : IModelFirstPropertyCollector
{
    public (IReadOnlyList<EntityProperty> Properties, string TableName)? Collect(string solutionDir, string rootNamespace, DbFlavor dbFlavor)
    {
        var defaultSchema = dbFlavor == DbFlavor.Postgres ? "public" : "dbo";

        var schema = console.Prompt(
            new TextPrompt<string>(string.Format(SR.Current.SchemaOwnerPrompt, defaultSchema))
                .DefaultValue(defaultSchema)
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(schema))
            schema = defaultSchema;

        var tableName = console.Prompt(
            new TextPrompt<string>(SR.Current.TableNamePrompt)
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error(SR.Current.TableNameRequired)
                    : ValidationResult.Success()));

        var connString = connectionStringReader.Read(solutionDir, rootNamespace);

        if (string.IsNullOrWhiteSpace(connString))
        {
            connString = console.Prompt(
                new TextPrompt<string>(SR.Current.ConnectionStringNotFound)
                    .Validate(s => string.IsNullOrWhiteSpace(s)
                        ? ValidationResult.Error(SR.Current.ConnectionStringRequired)
                        : ValidationResult.Success()));
        }

        (IReadOnlyList<EntityProperty>? Columns, Exception? Error)? result = null;

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(string.Format(SR.Current.ReadingTableStructure, schema, tableName), _ =>
            {
                try
                {
                    result = (dbSchemaReader.ReadColumns(connString!, schema, tableName, dbFlavor), null);
                }
                catch (Exception ex)
                {
                    result = (null, ex);
                }
            });

        if (result?.Error is { } error)
        {
            console.MarkupLine(string.Format(SR.Current.ErrorReadingTable, Markup.Escape(error.Message)));
            return null;
        }

        var properties = result?.Columns;

        if (properties is null || properties.Count == 0)
        {
            console.MarkupLine(string.Format(SR.Current.NoColumnsFound, schema, tableName));
            console.MarkupLine(SR.Current.CheckSchemaAndTableName);
            return null;
        }

        ShowSummaryTable(properties);
        return (properties, tableName);
    }

    private void ShowSummaryTable(IReadOnlyList<EntityProperty> properties)
    {
        console.WriteLine();

        var table = new Table()
            .AddColumn(SR.Current.ColProperty)
            .AddColumn(SR.Current.ColCsType)
            .AddColumn(SR.Current.ColNotNull);

        table.AddRow("Id", "int", "[green]✓ (PK)[/]");

        foreach (var p in properties)
        {
            table.AddRow(
                p.Name,
                p.CsType,
                p.IsRequired ? "[green]✓[/]" : "[grey]-[/]");
        }

        console.Write(table);
        console.WriteLine();
    }
}
