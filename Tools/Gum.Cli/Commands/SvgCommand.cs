using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullProjectPath}");
            return 2;
        }

        SvgExportResult result = ExportSvg(fullProjectPath, elementName, outputPath, width, height);

        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.ErrorMessage}");
            return 1;
        }

        Console.WriteLine($"SVG written to: {result.OutputPath}");
        return 0;
    }

    /// <summary>
    /// Runs the same export <see cref="Execute"/> uses, exposed as a public static method with a
    /// primitive-only signature so it can also be invoked by reflection across an
    /// <c>AssemblyLoadContext</c> boundary (see <c>Gum.Services.IsolatedPluginHost</c> and issue
    /// #4723) - the Avalonia head's SVG export menu item calls this in-process instead of shelling
    /// out to gumcli as a subprocess. Returns null on success, or an error message on failure;
    /// deliberately avoids throwing so a reflection caller never needs to know
    /// <see cref="SvgExportResult"/>'s type identity.
    /// </summary>
    public static string? ExportSvgInProcess(string projectPath, string elementName, string outputPath)
    {
        SvgExportResult result = ExportSvg(projectPath, elementName, outputPath, width: null, height: null);
        return result.Success ? null : result.ErrorMessage;
    }

    private static SvgExportResult ExportSvg(
        string projectPath, string elementName, string outputPath, int? width, int? height)
    {
        ISvgExportService service = new SkiaGumSvgExportService();

        SvgExportRequest request = new SvgExportRequest
        {
            ProjectPath = projectPath,
            ElementName = elementName,
            OutputPath = outputPath,
            Width = width,
            Height = height,
        };

        return service.ExportSvg(request);
    }
}
