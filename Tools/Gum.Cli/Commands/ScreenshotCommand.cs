using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.ProjectServices.MonoGame;
using Gum.ProjectServices.Raylib;
using Gum.ProjectServices.Screenshot;
using Gum.ProjectServices.SkiaGum;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli screenshot</c> command which renders a Gum Screen or Component to a PNG file.
/// </summary>
/// <remarks>
/// Defaults to MonoGame (DesktopGL); <c>--backend raylib</c> renders the same element via raylib
/// instead, so the two outputs can be diffed pixel-for-pixel against the same project (#4174) —
/// exactly the gap that let raylib's rendering silently diverge from the tool's undetected (#4172).
/// <c>--backend skia</c> renders through SkiaGum for games that draw with Skia (#7965).
/// All backends are referenced directly (see Gum.Cli.csproj) rather than loaded via a custom
/// AssemblyLoadContext: each `gumcli screenshot` invocation is a one-shot process that only ever
/// initializes one backend, so the static-singleton collision a shared-process concurrent-init would
/// risk never arises in practice.
/// </remarks>
public static class ScreenshotCommand
{
    private const string MonoGameBackend = "monogame";
    private const string RaylibBackend = "raylib";
    private const string SkiaBackend = "skia";

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
            $"Rendering backend to use: '{MonoGameBackend}' (default), '{RaylibBackend}' or '{SkiaBackend}'.");

        Option<string?> backgroundOption = new Option<string?>(
            "--background",
            "Background color to clear to before rendering, as 6-digit (RRGGBB) or 8-digit "
                + "(RRGGBBAA) hex, with or without a leading '#'. Defaults to fully transparent.");

        Option<string?> scaleOption = new Option<string?>(
            "--scale",
            "Whole number (1 or more) to enlarge the finished image by, with nearest-neighbor sampling. "
                + "The element still lays out at the canvas size or --width/--height. Defaults to 1.");

        Option<string[]> setOption = new Option<string[]>(
            "--set",
            "Override a variable for this render only, as Name=Value (Width=300, Hud.Text=Hello, "
                + "Hud.BossState=Visible). Repeatable; nothing is saved.")
        {
            AllowMultipleArgumentsPerToken = false,
        };

        Command command = new Command("screenshot", "Render a Gum Screen or Component to a PNG file.")
        {
            projectArgument,
            elementArgument,
            outputOption,
            widthOption,
            heightOption,
            backendOption,
            backgroundOption,
            scaleOption,
            setOption,
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
            string? scale = context.ParseResult.GetValueForOption(scaleOption);
            string[] sets = context.ParseResult.GetValueForOption(setOption) ?? Array.Empty<string>();

            string outputPath = output ?? $"{elementName}.png";

            context.ExitCode = Execute(projectPath, elementName, outputPath, width, height, backend, background, scale, sets);
        });

        return command;
    }

    private static int Execute(
        string projectPath, string elementName, string outputPath, int? width, int? height, string backend,
        string? background, string? scaleText, string[] sets)
    {
        int scale = 1;
        if (scaleText != null && (!int.TryParse(scaleText, out scale) || scale < 1))
        {
            Console.Error.WriteLine($"error: Invalid --scale value '{scaleText}'. Expected a whole number of 1 or more.");
            return 2;
        }

        List<ScreenshotVariableOverride> overrides = new List<ScreenshotVariableOverride>();
        foreach (string set in sets)
        {
            if (!ScreenshotVariableOverride.TryParse(set, out ScreenshotVariableOverride? variableOverride))
            {
                Console.Error.WriteLine($"error: Invalid --set value '{set}'. Expected Name=Value, such as Width=300.");
                return 2;
            }
            overrides.Add(variableOverride!);
        }

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
            case SkiaBackend:
                service = new SkiaGumScreenshotService();
                break;
            default:
                Console.Error.WriteLine(
                    $"error: Unknown backend '{backend}'. Expected '{MonoGameBackend}', '{RaylibBackend}' or '{SkiaBackend}'.");
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
            VariableOverrides = overrides,
        };

        ScreenshotResult result = service.TakeScreenshot(request);

        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.ErrorMessage}");
            return 1;
        }

        // Scaling the finished PNG works the same for every backend.
        PngUpscaler.ScaleFile(result.OutputPath!, scale);

        Console.WriteLine($"Screenshot written to: {result.OutputPath}");
        return 0;
    }
}
