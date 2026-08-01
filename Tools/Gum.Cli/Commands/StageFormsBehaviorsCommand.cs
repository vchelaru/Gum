using System.CommandLine;
using System.CommandLine.Invocation;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli stage-forms-behaviors</c> command, which resolves a Forms theme's
/// behaviors (following BehaviorReference.SourcePath links and DefaultImplementationOverride
/// values) and writes each as a flat, standalone .behx file. Used by GumFormsPlugin's postbuild
/// step to stage the Standard/Bubblegum themes' true effective behaviors for Content/FormsThemes,
/// since a plain xcopy can't follow shared-file links or apply per-theme overrides. See #4070.
/// </summary>
public static class StageFormsBehaviorsCommand
{
    /// <summary>
    /// Creates the <c>stage-forms-behaviors</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the theme's .gumx project file.");

        var destinationArgument = new Argument<string>(
            "destination",
            "Directory to write resolved .behx files into; created if missing.");

        var command = new Command(
            "stage-forms-behaviors",
            "Resolve a Forms theme's behaviors (following shared-file links and overrides) and write them flat into a destination folder.")
        {
            projectArgument,
            destinationArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string destinationPath = context.ParseResult.GetValueForArgument(destinationArgument);
            context.ExitCode = Execute(projectPath, destinationPath);
        });

        return command;
    }

    private static int Execute(string projectPath, string destinationPath)
        => FormsThemeBehaviorStagingRunner.Run(projectPath, destinationPath);
}
