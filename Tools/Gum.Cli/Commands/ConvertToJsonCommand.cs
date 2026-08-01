using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli convert-to-json</c> command which writes a JSON (<c>.gumj</c>) sibling of
/// a project - and JSON siblings for every Screen/Component/StandardElement/Behavior/animation file
/// it references - without touching the existing XML (issue #4175).
/// </summary>
public static class ConvertToJsonCommand
{
    /// <summary>
    /// Creates the <c>convert-to-json</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        Command command = new Command(
            "convert-to-json",
            "Convert a Gum project (and every element/behavior/animation file it references) to JSON, leaving the existing XML untouched.")
        {
            projectArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            context.ExitCode = Execute(projectPath);
        });

        return command;
    }

    private static int Execute(string projectPath)
    {
        string fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        GumProjectSave project = loadResult.Project!;
        IConvertProjectToJsonService convertService = new ConvertProjectToJsonService(new HeadlessFileWatchIgnoreList());

        ConvertProjectToJsonResult result;
        try
        {
            result = convertService.ConvertToJson(project);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        Console.WriteLine($"Wrote {result.ProjectFilePath}");
        Console.WriteLine(
            $"Converted {result.ScreenCount} screen(s), {result.ComponentCount} component(s), " +
            $"{result.StandardElementCount} standard element(s), {result.BehaviorCount} behavior(s), " +
            $"{result.AnimationCount} animation(s) - {result.TotalFileCount} file(s) total.");

        return 0;
    }
}
