using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using Gum.ProjectServices.SkiaGum;
using Gum.ProjectServices.SvgExport;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli svg</c> command which renders a Gum Screen or Component to an SVG file.
/// </summary>
/// <remarks>
/// Uses SkiaGum to render the element to an <c>SKSvgCanvas</c>, producing a vector SVG file.
/// Bitmap content (sprites, textures) will be embedded as base64-encoded images in the SVG.
/// </remarks>
public static class SvgCommand
{
    /// <summary>
    /// Creates the <c>svg</c> command definition.
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
            "Path for the output SVG file. Defaults to <element>.svg in the current directory.");

        Option<int?> widthOption = new Option<int?>(
            "--width",
            "Width of the output SVG in pixels. Defaults to the project canvas width.");

        Option<int?> heightOption = new Option<int?>(
            "--height",
            "Height of the output SVG in pixels. Defaults to the project canvas height.");

        Command command = new Command("svg", "Render a Gum Screen or Component to an SVG file.")
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

            string outputPath = output ?? $"{elementName}.svg";

            context.ExitCode = Execute(projectPath, elementName, outputPath, width, height);
        });

        return command;
    }

    private static int Execute(string projectPath, string elementName, string outputPath, int? width, int? height)
    {
        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        Console.WriteLine($"gumcli v{version}");

        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullProjectPath}");
            return 2;
        }

        ISvgExportService service = new SkiaGumSvgExportService();

        SvgExportRequest request = new SvgExportRequest
        {
            ProjectPath = fullProjectPath,
            ElementName = elementName,
            OutputPath = outputPath,
            Width = width,
            Height = height,
        };

        SvgExportResult result = service.ExportSvg(request);

        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.ErrorMessage}");
            return 1;
        }

        Console.WriteLine($"SVG written to: {result.OutputPath}");
        return 0;
    }
}
