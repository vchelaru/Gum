using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using ToolsUtilities;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli fonts</c> command which generates missing bitmap font files for a project.
/// Supports both bmfont.exe (Windows-only) and KernSmith (cross-platform) backends.
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

        Command command = new Command("fonts", "Generate missing bitmap font files for a Gum project.")
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
        IFontFileGenerator fontFileGenerator = CreateFontFileGenerator(project, callbacks);
        IHeadlessFontGenerationService fontService = new HeadlessFontGenerationService(fontFileGenerator, callbacks);

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
    /// Selects the appropriate font file generator based on the project's <see cref="GumProjectSave.FontGenerator"/> setting.
    /// </summary>
    private static IFontFileGenerator CreateFontFileGenerator(GumProjectSave project, IFontGenerationCallbacks callbacks)
    {
        return project.FontGenerator switch
        {
            FontGeneratorType.KernSmith => new KernSmithFileGenerator(callbacks),
            _ => new BmFontExeFileGenerator(callbacks)
        };
    }

    /// <summary>
    /// Routes font generation output to the console.
    /// </summary>
    private sealed class ConsoleFontGenerationCallbacks : IFontGenerationCallbacks
    {
        public void OnOutput(string message) => Console.WriteLine(message);
    }
}
