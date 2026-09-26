using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli resave</c> command, which loads a project and saves it again with no
/// edits: the project file plus every Screen, Component and Standard element. Comparing the files
/// before and after shows whether a load/save round trip changes a project (issue #5116).
/// By default the project loads the way the tool loads it, including the load-time back-fill of
/// standard-element defaults (<c>GumProjectSave.Initialize</c>), so the save shows what opening and
/// saving in the tool would write. <c>--raw</c> skips that step, so the save exercises only the
/// serializers and should reproduce every file byte for byte.
/// </summary>
public static class ResaveCommand
{
    /// <summary>
    /// Creates the <c>resave</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx or .gumj project file.");

        Option<bool> rawOption = new Option<bool>(
            "--raw",
            "Skip the tool's load-time back-fill of standard-element defaults, so only the serializers run.");

        Command command = new Command(
            "resave",
            "Load a Gum project and save it again without changes (project, screens, components and standards).")
        {
            projectArgument,
            rawOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            bool raw = context.ParseResult.GetValueForOption(rawOption);
            context.ExitCode = Execute(projectPath, raw);
        });

        return command;
    }

    private static int Execute(string projectPath, bool raw)
    {
        string fullPath = Path.GetFullPath(projectPath);
        return raw ? ExecuteRaw(fullPath) : ExecuteAsTool(fullPath);
    }

    private static int ExecuteRaw(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Project file not found: {fullPath}");
            return 2;
        }

        GumProjectSave? project = GumProjectSave.Load(fullPath, out GumLoadResult loadResult);
        if (project == null)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage ?? "Failed to load project");
            return 2;
        }

        return Save(project, fullPath);
    }

    private static int ExecuteAsTool(string fullPath)
    {
        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        foreach (ErrorResult loadError in loadResult.LoadErrors)
        {
            Console.Error.WriteLine($"warning: {loadError.ElementName}: {loadError.Message}");
        }

        return Save(loadResult.Project!, fullPath);
    }

    private static int Save(GumProjectSave project, string fullPath)
    {
        try
        {
            project.Save(fullPath, saveElements: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"Saved {fullPath}");
        return 0;
    }
}
