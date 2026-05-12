using System.Diagnostics.CodeAnalysis;
using OpenBase.CLI.Models;
using Spectre.Console;

namespace OpenBase.CLI.Helpers;

[ExcludeFromCodeCoverage]
public sealed class ConsoleModelFirstPropertyCollector(
    IAnsiConsole console,
    IDbSchemaReader dbSchemaReader,
    IConnectionStringReader connectionStringReader)
    : IModelFirstPropertyCollector
{
    public IReadOnlyList<EntityProperty>? Collect(string solutionDir, string rootNamespace, DbFlavor dbFlavor)
    {
        var defaultSchema = dbFlavor == DbFlavor.Postgres ? "public" : "dbo";

        var schema = console.Prompt(
            new TextPrompt<string>($"Schema/owner [{defaultSchema}]:")
                .DefaultValue(defaultSchema)
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(schema))
            schema = defaultSchema;

        var tableName = console.Prompt(
            new TextPrompt<string>("Nome da tabela:")
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error("Informe o nome da tabela.")
                    : ValidationResult.Success()));

        var connString = connectionStringReader.Read(solutionDir, rootNamespace);

        if (string.IsNullOrWhiteSpace(connString))
        {
            connString = console.Prompt(
                new TextPrompt<string>("[yellow]Connection string não encontrada no appsettings.json.[/]\nInforme a connection string:")
                    .Validate(s => string.IsNullOrWhiteSpace(s)
                        ? ValidationResult.Error("A connection string é obrigatória.")
                        : ValidationResult.Success()));
        }

        IReadOnlyList<EntityProperty>? properties = null;
        Exception? readError = null;

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Lendo estrutura da tabela [blue]{schema}.{tableName}[/]...", _ =>
            {
                try
                {
                    properties = dbSchemaReader.ReadColumns(connString!, schema, tableName, dbFlavor);
                }
                catch (Exception ex)
                {
                    readError = ex;
                }
            });

        if (readError is not null)
        {
            console.MarkupLine($"[red]Erro ao ler a tabela:[/] {Markup.Escape(readError.Message)}");
            return null;
        }

        if (properties is null || properties.Count == 0)
        {
            console.MarkupLine($"[red]Nenhuma coluna encontrada na tabela [bold]{schema}.{tableName}[/].[/]");
            console.MarkupLine("Verifique se o nome do schema e da tabela estão corretos.");
            return null;
        }

        ShowSummaryTable(properties);
        return properties;
    }

    private void ShowSummaryTable(IReadOnlyList<EntityProperty> properties)
    {
        console.WriteLine();

        var table = new Table()
            .AddColumn("Propriedade")
            .AddColumn("Tipo C#")
            .AddColumn("Not Null");

        table.AddRow("Id", "int", "[green]Sim (PK)[/]");

        foreach (var p in properties)
        {
            table.AddRow(
                p.Name,
                p.CsType,
                p.IsRequired ? "[green]Sim[/]" : "[grey]Não[/]");
        }

        console.Write(table);
        console.WriteLine();
    }
}
