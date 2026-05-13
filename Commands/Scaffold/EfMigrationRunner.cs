using OpenBase.CLI.Helpers.Execution;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;

namespace OpenBase.CLI.Commands.Scaffold;

internal sealed class EfMigrationRunner(IDotNetRunner dotNetRunner, IFileWriter fileWriter, IAnsiConsole console)
{
    public void RunMigrations(ScaffoldContext ctx, string entity)
    {
        RestorePackages(ctx);

        var (migrationOk, migrationError) = RunEfCommand(
            $"migrations add Add{entity}",
            string.Format(SR.Current.GeneratingMigration, entity),
            ctx);

        if (!migrationOk)
        {
            console.MarkupLine(SR.Current.MigrationFailed);
            if (!string.IsNullOrWhiteSpace(migrationError))
                console.MarkupLine($"[grey]{Markup.Escape(migrationError)}[/]");
            console.MarkupLine(string.Format(SR.Current.RunMigrationManually, entity));
            return;
        }

        console.MarkupLine(string.Format(SR.Current.MigrationGenerated, entity));

        if (!console.Profile.Capabilities.Interactive ||
            !console.Confirm(SR.Current.RunDatabaseUpdateNow, defaultValue: true))
            return;

        var (updateOk, updateError) = RunEfCommand("database update", SR.Current.ExecutingDatabaseUpdate, ctx);

        if (!updateOk)
        {
            console.MarkupLine(SR.Current.DatabaseUpdateFailed);
            if (!string.IsNullOrWhiteSpace(updateError))
                console.MarkupLine($"[grey]{Markup.Escape(updateError)}[/]");
            console.MarkupLine(SR.Current.DotnetEfDatabaseUpdate);
            return;
        }

        console.MarkupLine(SR.Current.DatabaseUpdatedSuccess);
    }

    public void RunReconciliationMigration(ScaffoldContext ctx, string entity)
    {
        RestorePackages(ctx);

        var (migOk, migError) = RunEfCommand(
            $"migrations add Add{entity}",
            string.Format(SR.Current.ModelFirstReconciliationInfo, entity),
            ctx);

        if (!migOk)
        {
            console.MarkupLine(string.Format(SR.Current.ModelFirstReconciliationWarn, entity));
            if (!string.IsNullOrWhiteSpace(migError))
                console.MarkupLine($"[grey]{Markup.Escape(migError)}[/]");
            return;
        }

        var migrationsDir = Path.Combine(ctx.InfraContextPath, "Migrations");
        var migFile = fileWriter.FindFile(migrationsDir, $"*_Add{entity}.cs");

        if (migFile is not null)
        {
            var patched = DbContextEditor.EmptyMigrationUpMethod(fileWriter.ReadAllText(migFile));
            fileWriter.WriteAllText(migFile, patched);
        }

        var (updateOk, updateError) = RunEfCommand("database update", SR.Current.ExecutingDatabaseUpdate, ctx);

        if (!updateOk)
        {
            console.MarkupLine(string.Format(SR.Current.ModelFirstReconciliationWarn, entity));
            if (!string.IsNullOrWhiteSpace(updateError))
                console.MarkupLine($"[grey]{Markup.Escape(updateError)}[/]");
            return;
        }

        console.MarkupLine(SR.Current.ModelFirstReconciliationSuccess);
    }

    private void RestorePackages(ScaffoldContext ctx)
    {
        (bool Success, string Error)? result = null;
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(SR.Current.RestoringNuGetPackages, _ =>
            {
                result = dotNetRunner.Run($"restore \"{ctx.SolutionDir}\"");
            });

        if (result is { Success: false })
        {
            console.MarkupLine(SR.Current.RestorePackagesWarning);
            if (!string.IsNullOrWhiteSpace(result.Value.Error))
                console.MarkupLine($"[grey]{Markup.Escape(result.Value.Error)}[/]");
        }
    }

    private (bool Success, string Error) RunEfCommand(string efArgs, string spinnerLabel, ScaffoldContext ctx)
    {
        var projectArg = $"--project \"{ctx.InfraContextPath}\" --startup-project \"{ctx.PresentationPath}\"";
        (bool Success, string Error)? result = null;

        console.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(spinnerLabel, _ =>
            {
                result = dotNetRunner.Run($"ef {efArgs} {projectArg}");
            });

        return result ?? (false, string.Empty);
    }
}
