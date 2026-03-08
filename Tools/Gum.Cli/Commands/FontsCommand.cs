using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Runtime.InteropServices;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using ToolsUtilities;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli fonts</c> command which generates missing bitmap font files for a project.
/// Windows-only: bmfont.exe is a Windows-only application.
/// </summary>
public static class FontsCommand
{
    /// <summary>
    /// Creates the <c>fonts</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        Command command = new Command("fonts", "Generate missing bitmap font files for a Gum project. Windows-only.")
        {
            projectArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            context.ExitCode = Execute(projectPath).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async System.Threading.Tasks.Task<int> Execute(string projectPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("error: Font generation requires Windows (bmfont.exe is a Windows-only application).");
            return 2;
        }

        string fullPath = Path.GetFullPath(projectPath);

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

        GumProjectSave project = loadResult.Project!;
        string projectDirectory = Path.GetDirectoryName(fullPath)! + Path.DirectorySeparatorChar;

        StandardElementsManager.Self.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        FileManager.RelativeDirectory = projectDirectory;

        IFontGenerationCallbacks callbacks = new ConsoleFontGenerationCallbacks();
        IHeadlessFontGenerationService fontService = new HeadlessFontGenerationService(callbacks);

        try
        {
            await fontService.CreateAllMissingFontFiles(project, projectDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Routes font generation output to the console.
    /// </summary>
    private sealed class ConsoleFontGenerationCallbacks : IFontGenerationCallbacks
    {
        public void OnOutput(string message) => Console.WriteLine(message);
    }
}
