using OpenBase.CLI.Commands.Scaffold;
using OpenBase.CLI.Helpers.IO;
using OpenBase.CLI.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenBase.CLI.Commands;

public class SpecialistSettings : EntityCommandSettings { }

public class SpecialistCommand(
    IAnsiConsole console,
    IProjectLocator projectLocator,
    IFileWriter fileWriter)
    : Command<SpecialistSettings>
{
    protected override int Execute(CommandContext context, SpecialistSettings settings, CancellationToken cancellationToken)
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

        var ctx       = new ScaffoldContext(settings.Entity, rootNamespace, solutionDir);
        var generator = new ScaffoldGenerator(ctx);
        var wizard    = new SpecialistWizard(console);
        var helper    = new ScaffoldFileHelper(console, fileWriter);

        do
        {
            var definition = wizard.AskDefinition();
            if (definition is null) return 0;

            var files = generator.GetSpecialistFiles(definition).ToList();
            var (created, skipped, failed) = helper.WriteFiles(files, solutionDir, settings.Entity);

            helper.PrintFileList(string.Format(SR.Current.SpecialistFilesCreated, created.Count), created, "green");
            helper.PrintFileList(string.Format(SR.Current.FilesSkipped, skipped.Count), skipped, "yellow");
            if (failed.Count > 0)
                helper.PrintFileList(string.Format(SR.Current.FilesErrors, failed.Count), failed, "red", "red");

        } while (console.Confirm(SR.Current.SpecialistAddAnother, defaultValue: false));

        return 0;
    }
}
