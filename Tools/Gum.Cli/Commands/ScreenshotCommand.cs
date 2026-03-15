using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Screenshot;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli screenshot</c> command which renders a Gum Screen or Component to a PNG file.
/// </summary>
/// <remarks>
/// Uses MonoGame (DesktopGL) to render the element with the same backend as a MonoGame game,
/// producing pixel-accurate output suitable for visual regression testing.
/// <para>
/// FUTURE: When a second backend is added (e.g. Skia), do not add it as a direct ProjectReference.
/// Both backends compile copies of shared types (RenderingLibrary, SystemManagers, etc.) and share
/// static singletons via GumCommon. Instead, load backends dynamically using a custom
/// AssemblyLoadContext + AssemblyDependencyResolver so each backend's dependencies are isolated
/// and only one is loaded per process run. Add a --backend flag to select between them.
/// See the comment in Gum.Cli.csproj for more detail.
/// </para>
/// </remarks>
public static class ScreenshotCommand
{

    /// <summary>
    /// Creates the <c>screenshot</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        Argument<string> elementArgument = new Argument<string>(
            "element",
            "Name of the Screen or Component to render.");

        Option<string> outputOption = new Option<string>(
            "--output",
            "Path for the output PNG file. Defaults to <element>.png in the current directory.");

        Option<int?> widthOption = new Option<int?>(
            "--width",
            "Width of the output image in pixels. Defaults to the project canvas width.");

        Option<int?> heightOption = new Option<int?>(
            "--height",
            "Height of the output image in pixels. Defaults to the project canvas height.");

        Command command = new Command("screenshot", "Render a Gum Screen or Component to a PNG file.")
        {
            projectArgument,
            elementArgument,
            outputOption,
            widthOption,
            heightOption,
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string elementName = context.ParseResult.GetValueForArgument(elementArgument);
            string? output = context.ParseResult.GetValueForOption(outputOption);
            int? width = context.ParseResult.GetValueForOption(widthOption);
            int? height = context.ParseResult.GetValueForOption(heightOption);

            string outputPath = output ?? $"{elementName}.png";

            context.ExitCode = Execute(projectPath, elementName, outputPath, width, height);
        });

        return command;
    }

    private static int Execute(string projectPath, string elementName, string outputPath, int? width, int? height)
    {
        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullProjectPath}");
            return 2;
        }

        IScreenshotService service = new MonoGameScreenshotService();

        ScreenshotRequest request = new ScreenshotRequest
        {
            ProjectPath = fullProjectPath,
            ElementName = elementName,
            OutputPath = outputPath,
            Width = width,
            Height = height,
        };

        ScreenshotResult result = service.TakeScreenshot(request);

        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.ErrorMessage}");
            return 1;
        }

        Console.WriteLine($"Screenshot written to: {result.OutputPath}");
        return 0;
    }
}
