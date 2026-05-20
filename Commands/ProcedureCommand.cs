using System.ComponentModel;
using OpenBase.CLI.Commands.Procedure;
using OpenBase.CLI.Helpers.Database;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using OpenBase.CLI.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class ProcedureSettings : CommandSettings
{
    [CommandOption("-n|--name <NAME>")]
    [Description("O nome da procedure/package (PascalCase, ex: GetOrderById)")]
    public string? Name { get; set; }

    [CommandOption("-s|--schema <SCHEMA>")]
    [Description("Schema/owner no banco de dados (detectado automaticamente se omitido)")]
    public string? Schema { get; set; }

    [CommandOption("--namespace <NAMESPACE>")]
    [Description("Namespace raiz do projeto (detectado automaticamente se omitido)")]
    public string? RootNamespace { get; set; }
}

public class ProcedureCommand(
    IAnsiConsole console,
    IProjectLocator projectLocator,
    IFileWriter fileWriter,
    IDbFlavorDetector dbFlavorDetector,
    IConnectionStringReader connectionStringReader,
    IDbSchemaReader dbSchemaReader)
    : Command<ProcedureSettings>
{
    protected override int Execute(CommandContext context, ProcedureSettings settings, CancellationToken cancellationToken)
    {
        var (solutionDir, rootNamespace) = projectLocator.Detect(
            Directory.GetCurrentDirectory(), settings.RootNamespace);

        if (solutionDir is null || rootNamespace is null)
        {
            console.MarkupLine(SR.Current.ProjectStructureNotFound);
            console.MarkupLine(SR.Current.RunInProjectRoot);
            console.MarkupLine(SR.Current.OrProvideNamespace);
            return 1;
        }

        var dbFlavor = dbFlavorDetector.Detect(solutionDir);
        var connStr  = connectionStringReader.Read(solutionDir, rootNamespace);
        var schema   = settings.Schema ?? DefaultSchema(dbFlavor);

        string procedureName;
        IReadOnlyList<ProcedureParameter> parameters;

        if (!string.IsNullOrWhiteSpace(settings.Name))
        {
            if (!char.IsUpper(settings.Name[0]) || !settings.Name.All(char.IsLetterOrDigit))
            {
                console.MarkupLine(SR.Current.ProcedureNameMustBePascalCase);
                return 1;
            }

            procedureName = settings.Name;
            parameters = TryReadParametersFromDb(connStr, schema, procedureName, dbFlavor);
        }
        else if (console.Profile.Capabilities.Interactive && connStr is not null)
        {
            var selected = SelectProcedureInteractively(connStr, dbFlavor, ref schema);
            if (selected is null) return 1;
            (procedureName, parameters) = selected.Value;
        }
        else
        {
            console.MarkupLine(SR.Current.ProcedureNameRequired);
            return 1;
        }

        ShowParametersTable(parameters);

        if (console.Profile.Capabilities.Interactive && !console.Confirm(SR.Current.ProceedWithScaffold, defaultValue: true))
            return 0;

        var testsPath = DetectTestsPath(solutionDir, rootNamespace);
        var ctx = new ProcedureContext(procedureName, rootNamespace, solutionDir)
        {
            Parameters = parameters,
            TestsPath  = testsPath
        };

        var generator = new ProcedureGenerator(ctx);
        var files = generator.GetFiles().ToList();
        var (created, skipped, failed) = WriteFiles(files, solutionDir, procedureName);

        PrintFileList(string.Format(SR.Current.FilesCreated, created.Count), created, "green");
        PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");

        if (failed.Count > 0)
        {
            PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");
            return 1;
        }

        if (created.Count > 0)
            console.MarkupLine(string.Format(SR.Current.ProcedureSuccess, procedureName));

        return 0;
    }


    private IReadOnlyList<ProcedureParameter> TryReadParametersFromDb(
        string? connStr, string schema, string procedureName, DbFlavor dbFlavor)
    {
        if (connStr is null) return [];

        try
        {
            IReadOnlyList<ProcedureParameter> result = [];

            console.Status()
                .Spinner(Spinner.Known.Dots)
                .Start(string.Format(SR.Current.ProcedureReadingParams, schema, procedureName), _ =>
                {
                    result = dbSchemaReader.ReadProcedureParameters(connStr, schema, procedureName, dbFlavor);
                });

            if (result.Count == 0)
                console.MarkupLine(SR.Current.ProcedureNoParamsFound);

            return result;
        }
        catch
        {
            console.MarkupLine(SR.Current.ProcedureNoParamsFound);
            return [];
        }
    }

    private (string Name, IReadOnlyList<ProcedureParameter> Parameters)? SelectProcedureInteractively(
        string connStr, DbFlavor dbFlavor, ref string schema)
    {
        IReadOnlyList<DbProcedureInfo> procs = [];

        try
        {
            console.Status()
                .Spinner(Spinner.Known.Dots)
                .Start(SR.Current.ProcedureListingProcs, _ =>
                {
                    procs = dbSchemaReader.ListProcedures(connStr, dbFlavor);
                });
        }
        catch (Exception ex)
        {
            console.MarkupLine(string.Format(SR.Current.ErrorReadingTable, Markup.Escape(ex.Message)));
            return null;
        }

        if (procs.Count == 0)
        {
            console.MarkupLine(SR.Current.ProcedureNoProcsFound);
            return null;
        }

        console.MarkupLine(string.Format(SR.Current.TablesFound, procs.Count));

        var choices = procs.Select(p => $"{p.Schema}.{p.Name}").ToList();
        var selected = console.Prompt(
            new SelectionPrompt<string>()
                .Title(SR.Current.ProcedureSelectPrompt)
                .PageSize(15)
                .AddChoices(choices));

        var parts = selected.Split('.', 2);
        schema = parts[0];
        var procName = SqlTypeMapper.ToPascalCase(parts[1]);

        var parameters = TryReadParametersFromDb(connStr, schema, parts[1], dbFlavor);
        return (procName, parameters);
    }

    private void ShowParametersTable(IReadOnlyList<ProcedureParameter> parameters)
    {
        if (parameters.Count == 0) return;

        console.WriteLine();
        var table = new Table()
            .AddColumn(SR.Current.ProcedureColParam)
            .AddColumn(SR.Current.ProcedureColDirection)
            .AddColumn(SR.Current.ColCsType);

        foreach (var p in parameters)
        {
            var dir = p.Direction switch
            {
                ParameterDirection.In    => "[green]IN[/]",
                ParameterDirection.Out   => "[blue]OUT[/]",
                ParameterDirection.InOut => "[yellow]IN/OUT[/]",
                _                        => p.Direction.ToString()
            };
            table.AddRow(p.Name, dir, p.CsType);
        }

        console.Write(table);
        console.WriteLine();
    }

    private string DetectTestsPath(string solutionDir, string rootNamespace)
    {
        var testsRoot = Path.Combine(solutionDir, "tests");
        var csprojName = $"{rootNamespace}.Tests.Unit.csproj";
        var found = fileWriter.FindFile(testsRoot, csprojName);
        return found is not null
            ? Path.GetDirectoryName(found)!
            : Path.Combine(testsRoot, $"{rootNamespace}.Tests.Unit");
    }

    private (List<string> Created, List<string> Skipped, List<string> Failed) WriteFiles(
        IEnumerable<(string Path, string Content)> files,
        string solutionDir,
        string procedureName)
    {
        var created = new List<string>();
        var skipped = new List<string>();
        var failed  = new List<string>();

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(string.Format(SR.Current.GeneratingScaffold, procedureName), _ =>
            {
                foreach (var (path, content) in files)
                {
                    var rel = Path.GetRelativePath(solutionDir, path);
                    try
                    {
                        if (fileWriter.FileExists(path))
                        {
                            skipped.Add(rel);
                            continue;
                        }
                        fileWriter.EnsureDirectory(Path.GetDirectoryName(path)!);
                        fileWriter.WriteAllText(path, content);
                        created.Add(rel);
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{rel}: {ex.Message}");
                    }
                }
            });

        return (created, skipped, failed);
    }

    private void PrintFileList(string header, List<string> files, string color, string itemColor = "grey")
    {
        if (files.Count == 0) return;
        console.MarkupLine($"[{color}]{Markup.Escape(header)}[/]");
        foreach (var f in files)
            console.MarkupLine($"  [{itemColor}]{Markup.Escape(f)}[/]");
    }

    private static string DefaultSchema(DbFlavor dbFlavor) => dbFlavor switch
    {
        DbFlavor.Postgres => "public",
        DbFlavor.Oracle   => string.Empty,
        _                 => "dbo"
    };
}
