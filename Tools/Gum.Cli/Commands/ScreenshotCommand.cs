using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Raylib;
using Gum.ProjectServices.Screenshot;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli screenshot</c> command which renders a Gum Screen or Component to a PNG file.
/// </summary>
/// <remarks>
/// Defaults to MonoGame (DesktopGL); <c>--backend raylib</c> renders the same element via raylib
/// instead, so the two outputs can be diffed pixel-for-pixel against the same project (#4174) —
/// exactly the gap that let raylib's rendering silently diverge from the tool's undetected (#4172).
/// Both backends are referenced directly (see Gum.Cli.csproj) rather than loaded via a custom
/// AssemblyLoadContext: each `gumcli screenshot` invocation is a one-shot process that only ever
/// initializes one backend, so the static-singleton collision a shared-process concurrent-init would
/// risk never arises in practice.
/// </remarks>
public static class ScreenshotCommand
{
    private const string MonoGameBackend = "monogame";
    private const string RaylibBackend = "raylib";

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

        Option<string> backendOption = new Option<string>(
            "--backend",
            () => MonoGameBackend,
            $"Rendering backend to use: '{MonoGameBackend}' (default) or '{RaylibBackend}'.");

        Option<string?> backgroundOption = new Option<string?>(
            "--background",
            "Background color to clear to before rendering, as 6-digit (RRGGBB) or 8-digit "
                + "(RRGGBBAA) hex, with or without a leading '#'. Defaults to fully transparent.");

        Command command = new Command("screenshot", "Render a Gum Screen or Component to a PNG file.")
        {
            projectArgument,
            elementArgument,
            outputOption,
            widthOption,
            heightOption,
            backendOption,
            backgroundOption,
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string elementName = context.ParseResult.GetValueForArgument(elementArgument);
            string? output = context.ParseResult.GetValueForOption(outputOption);
            int? width = context.ParseResult.GetValueForOption(widthOption);
            int? height = context.ParseResult.GetValueForOption(heightOption);
            string backend = context.ParseResult.GetValueForOption(backendOption) ?? MonoGameBackend;
            string? background = context.ParseResult.GetValueForOption(backgroundOption);

            string outputPath = output ?? $"{elementName}.png";

            context.ExitCode = Execute(projectPath, elementName, outputPath, width, height, backend, background);
        });

        return command;
    }

    private static int Execute(
        string projectPath, string elementName, string outputPath, int? width, int? height, string backend,
        string? background)
    {
        ScreenshotColor? backgroundColor = null;
        if (background != null)
        {
            if (!ScreenshotColor.TryParse(background, out ScreenshotColor parsedColor))
            {
                Console.Error.WriteLine(
                    $"error: Invalid --background value '{background}'. Expected 6-digit (RRGGBB) or "
                        + "8-digit (RRGGBBAA) hex, with or without a leading '#'.");
                return 2;
            }

            backgroundColor = parsedColor;
        }

        string fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullProjectPath}");
            return 2;
        }

        IScreenshotService service;
        switch (backend.ToLowerInvariant())
        {
            case MonoGameBackend:
                service = new MonoGameScreenshotService();
                break;
            case RaylibBackend:
                service = new RaylibScreenshotService();
                break;
            default:
                Console.Error.WriteLine(
                    $"error: Unknown backend '{backend}'. Expected '{MonoGameBackend}' or '{RaylibBackend}'.");
                return 2;
        }

        ScreenshotRequest request = new ScreenshotRequest
        {
            ProjectPath = fullProjectPath,
            ElementName = elementName,
            OutputPath = outputPath,
            Width = width,
            Height = height,
            BackgroundColor = backgroundColor,
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
