using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;

namespace OpenBase.CLI.Helpers.Interactive;

public sealed class ConsoleModelFirstPropertyCollector(
    IAnsiConsole console,
    IDbSchemaReader dbSchemaReader,
    IConnectionStringReader connectionStringReader)
    : IModelFirstPropertyCollector
{
    public (IReadOnlyList<EntityProperty> Properties, string TableName)? Collect(string solutionDir, string rootNamespace, DbFlavor dbFlavor)
    {
        var schema    = PromptSchema(dbFlavor);
        var tableName = PromptTableName();
        var connString = EnsureConnectionString(connectionStringReader.Read(solutionDir, rootNamespace));

        var (columns, error) = FetchColumns(connString, schema, tableName, dbFlavor);

        if (error is not null)
        {
            console.MarkupLine(string.Format(SR.Current.ErrorReadingTable, Markup.Escape(error.Message)));
            return null;
        }

        if (columns is null || columns.Count == 0)
        {
            console.MarkupLine(string.Format(SR.Current.NoColumnsFound, schema, tableName));
            console.MarkupLine(SR.Current.CheckSchemaAndTableName);
            return null;
        }

        ShowSummaryTable(columns);
        return (columns, tableName);
    }

    private string PromptSchema(DbFlavor dbFlavor)
    {
        var defaultSchema = dbFlavor == DbFlavor.Postgres ? "public" : "dbo";
        var prompt = new TextPrompt<string>(string.Format(SR.Current.SchemaOwnerPrompt, defaultSchema));

        if (dbFlavor == DbFlavor.Oracle)
            prompt.Validate(s => string.IsNullOrWhiteSpace(s)
                ? ValidationResult.Error(SR.Current.TableNameRequired)
                : ValidationResult.Success());
        else
            prompt.DefaultValue(defaultSchema).AllowEmpty();

        var schema = console.Prompt(prompt);
        return string.IsNullOrWhiteSpace(schema) ? defaultSchema : schema;
    }

    private string PromptTableName() =>
        console.Prompt(
            new TextPrompt<string>(SR.Current.TableNamePrompt)
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error(SR.Current.TableNameRequired)
                    : ValidationResult.Success()));

    private string EnsureConnectionString(string? connString)
    {
        if (!string.IsNullOrWhiteSpace(connString))
            return connString;

        return console.Prompt(
            new TextPrompt<string>(SR.Current.ConnectionStringNotFound)
                .Validate(s => string.IsNullOrWhiteSpace(s)
                    ? ValidationResult.Error(SR.Current.ConnectionStringRequired)
                    : ValidationResult.Success()));
    }

    private (IReadOnlyList<EntityProperty>? Columns, Exception? Error) FetchColumns(
        string connString, string schema, string tableName, DbFlavor dbFlavor)
    {
        (IReadOnlyList<EntityProperty>? Columns, Exception? Error) result = (null, null);

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(string.Format(SR.Current.ReadingTableStructure, schema, tableName), _ =>
            {
                try
                {
                    result = (dbSchemaReader.ReadColumns(connString, schema, tableName, dbFlavor), null);
                }
                catch (Exception ex)
                {
                    result = (null, ex);
                }
            });

        return result;
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
